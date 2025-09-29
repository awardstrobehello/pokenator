using System;
using System.Collections.Generic;
using System.Linq;
using Models;
using System.Text.Json;

// TestConfidence
// TestQuestion


namespace PokenatorBackend
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("!!!! POKENATOR LOGIC TEST !!!!\n");

            // RandomTest is a random pokemon and the akinator guesses random questions
            if (args.Length == 0)
            {
                RandomTest();
            }
            else
            {
                RealTest();
            }
        }

        static void RealTest()
        {
            Console.WriteLine("RealTest called");
            var engine = new LogicEngine();
            try
            {
                var (pokemon, question) = LoadData();
                Console.WriteLine($"{pokemon.Count} Pokemon; {question.Count} questions\n");
                QuestionSelection(engine, pokemon, question);
                TestConfidence(engine, pokemon, question);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
            }
        }

        static (List<Pokemon>, List<Question>) LoadData()
        {
            string pokemonPath = "Data/pokemon.json";
            string questionPath = "Data/questions.json";

            if (!File.Exists(questionPath) || (!File.Exists(pokemonPath)))
            {
                throw new FileNotFoundException($"File not found for {pokemonPath} or {questionPath}");
            }

            var pokemon = JsonSerializer.Deserialize<List<Pokemon>>(File.ReadAllText(pokemonPath));
            if (pokemon == null || !pokemon.Any())
            {
                throw new InvalidDataException("Pokemon data empty or invalid");
            }

            var questions = JsonSerializer.Deserialize<List<Question>>(File.ReadAllText(questionPath));
            if (questions == null || !questions.Any())
            {
                throw new InvalidDataException("Question data empty or invalid");
            }
            return (pokemon, questions);
        }


        static void QuestionSelection(LogicEngine engine, List<Pokemon> pokemon, List<Question> questions)
        {
            Console.WriteLine($"QuestionSelection called.");
            var emptyAnswers = new Dictionary<String, UserAnswer>();
            var bestQuestion = engine.SelectNextQuestion(pokemon, questions, emptyAnswers);

            if (bestQuestion != null)
            {
                Console.WriteLine($"Best first question: {bestQuestion.Text}");
                Console.WriteLine($"Category: {bestQuestion.Category}, attribute: {bestQuestion.TargetAttribute}\n");
            }
            else Console.WriteLine($"No question selected. Check for an issue...\n");

            Console.WriteLine("----- Top 7 candidates:");
            var tempAnswers = new Dictionary<String, UserAnswer>();

            for (int i = 0; i < 7; i++)
            {
                var tempQuestion = engine.SelectNextQuestion(pokemon, questions, tempAnswers);
                if (tempQuestion == null) break;

                tempAnswers[tempQuestion.Id] = new UserAnswer
                {
                    QuestionId = tempQuestion.Id,
                    Category = tempQuestion.Category,
                    TargetAttribute = tempQuestion.TargetAttribute,
                    Response = UserResponse.DontKnow
                };
                Console.WriteLine($"{i + 1}: {tempQuestion.Text}");
            }

        }

        static void RandomTest()
        {
            Console.WriteLine("RandomTest called\n");
            try
            {
                var engine = new LogicEngine();
                var (pokemon, questions) = LoadData();

                var random = new Random();

                int randomId = random.Next(pokemon.Count);
                var targetPokemon = pokemon[randomId - 1];
                Console.WriteLine($"Pokemon Id {randomId} ({targetPokemon.Name}) called.");

                // Random Game Test
                Console.WriteLine($"\n\n !!!!! Random test begun !!!!! \n\n");
                var userAnswers = new Dictionary<string, UserAnswer>();
                var remainingPokemon = new List<Pokemon>(pokemon);
                var askedQuestions = new HashSet<string>();

                for (int round = 1; round <= 8; round++)
                {
                    Console.WriteLine($"Round {round}");

                    var availableQuestions = questions.Where(q => !askedQuestions.Contains(q.Id)).ToList();

                    // Should NEVER happen
                    if (!availableQuestions.Any())
                    {
                        Console.WriteLine("No more questions to ask");
                        break;
                    }

                    var randomQuestion = availableQuestions[random.Next(availableQuestions.Count)];
                    askedQuestions.Add(randomQuestion.Id);
                    Console.WriteLine($"{randomQuestion}");
                    // Simulate target Pokemon's answer



                    var attributeDict = randomQuestion.Category.ToLower() switch
                    {
                        "type" => targetPokemon.Type,
                        "color" => targetPokemon.Color,
                        "other" => targetPokemon.Other,
                        _ => new Dictionary<string, double>()
                    };

                    var targetValue = attributeDict.TryGetValue(randomQuestion.TargetAttribute, out double value) ? value : 0.0;

                    var simulatedResponse = targetValue switch
                    {
                        >= 0.8 => UserResponse.Yes,
                        >= 0.6 => UserResponse.Somewhat,
                        >= 0.4 => UserResponse.DontKnow,
                        >= 0.2 => UserResponse.NotReally,
                        _ => UserResponse.No

                    };

                    Console.WriteLine($"Target Pokemon value: {targetValue}; Response {simulatedResponse}\n");



                    userAnswers[randomQuestion.Id] = new UserAnswer
                    {
                        QuestionId = randomQuestion.Id,
                        Category = randomQuestion.Category,
                        TargetAttribute = randomQuestion.TargetAttribute,
                        Response = simulatedResponse,
                    };

                    var confidences = pokemon
                        .Select(p => new
                        {
                            Pokemon = p,
                            Confidence = engine.CalculateConfidence(p, userAnswers)
                        })
                        .OrderByDescending(x => x.Confidence)
                        .Take(7)  // Top 7
                        .ToList();

                    Console.WriteLine("Top 7 candidates:");
                    foreach (var candidate in confidences)
                    {
                        string marker = candidate.Pokemon.Name == targetPokemon.Name ? " <<<<< TARGET" : "";
                        Console.WriteLine($"  {candidate.Pokemon.Name}: {candidate.Confidence:F3}{marker}");
                    }

                    var guess = engine.ShouldMakeGuess(remainingPokemon, userAnswers);
                    if (guess != null)
                    {
                        Console.WriteLine($"\nAI wants to guess: {guess.Name}");
                        bool correct = guess.Name == targetPokemon.Name;
                        Console.WriteLine(correct ? "Correct guess" : "Wrong guess");

                        if (correct)
                        {
                            Console.WriteLine($"Success in {round} questions");
                            return;
                        }
                        else
                        {
                            Console.WriteLine("Continuing with more questions...");
                            remainingPokemon.RemoveAll(p => p.Name == guess.Name);
                        }
                    }
                    Console.WriteLine();
                }
                Console.WriteLine("Simulation ended without a successful guess");
                var finalTop = pokemon
                    .Select(p => new
                    {
                        Pokemon = p,
                        Confidence = engine.CalculateConfidence(p, userAnswers)
                    })
                    .OrderByDescending(x => x.Confidence)
                    .First();

                Console.WriteLine($"Best final guess would be: {finalTop.Pokemon.Name} ({finalTop.Confidence:F3})");






            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
            }

        }

        //          \\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
        //          \\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
        //          \\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
        //          \\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
        //          \\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
        //          \\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
        //          \\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
        //          \\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\


        static void TestConfidence(LogicEngine engine, List<Pokemon> pokemon, List<Question> questions)
        {
            Console.WriteLine($"\nTestConfidenceWithData called");
            if (pokemon.Any() && questions.Any())
            {
                var testPokemon = pokemon[0];
                var testQuestion = questions[0];

                Console.WriteLine($"Testing with {testPokemon.Name} and question: {testQuestion.Text}");

                // Test different responses
                var responses = new[] { UserResponse.Yes, UserResponse.No, UserResponse.Somewhat, UserResponse.NotReally, UserResponse.DontKnow };

                foreach (var response in responses)
                {
                    var userAnswers = new Dictionary<string, UserAnswer>
                    {
                        [testQuestion.Id] = new UserAnswer
                        {
                            QuestionId = testQuestion.Id,
                            Category = testQuestion.Category,
                            TargetAttribute = testQuestion.TargetAttribute,
                            Response = response,
                        }
                    };

                    var confidence = engine.CalculateConfidence(testPokemon, userAnswers);
                    Console.WriteLine($"  Response '{response}': Confidence = {confidence}");
                }
            }
            Console.WriteLine();
        }

    }
}