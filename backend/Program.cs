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
                RealTest();
            }
            else
            {
                RandomTest();
            }
        }

        static void RealTest()
        {
            Console.WriteLine("RealTest called");
            
            try
            {
                var game = GameState.Initialize();

                Console.WriteLine($"{game.AllPokemon.Count} Pokemon; {game.AllQuestions.Count} questions\n");
                QuestionSelection(game);
                TestConfidence(game.Engine, game.AllPokemon, game.AllQuestions);
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


        static void QuestionSelection(GameState game)
        {
            Console.WriteLine($"QuestionSelection called.");
            var emptyAnswers = new Dictionary<String, UserAnswer>();
            var bestQuestion = game.GetNextQuestion();


            if (bestQuestion != null)
            {
                Console.WriteLine($"Best first question: {bestQuestion.Text}");
                Console.WriteLine($"Category: {bestQuestion.Category}, attribute: {bestQuestion.TargetAttribute}\n");
            }
            else Console.WriteLine($"No question selected. Check for an issue...\n");

            Console.WriteLine("----- Top 7 candidates:");

            for (int i = 0; i < 7; i++)
            {
                var tempQuestion = game.GetNextQuestion();
                if (tempQuestion == null) break;
                Console.WriteLine($"{i + 1}: {tempQuestion.Text}");
            }

        }

        static void RandomTest()
        {
            Console.WriteLine("RandomTest called\n");
            try
            {
                var game = GameState.Initialize();

                var random = new Random();

                int randomId = random.Next(game.AllPokemon.Count);
                var targetPokemon = game.AllPokemon[randomId];
                Console.WriteLine($"Target Pokemon: {targetPokemon.Name}\n");

                for (int round = 1; round <= 8; round++)
                {
                    Console.WriteLine($"Round {round}");


                    var question = game.GetNextQuestion();
                    if (question == null)
                    {
                        Console.WriteLine("No more questions available");
                        break;
                    }

                    Console.WriteLine($"{question.Text}");

                    var attributeDict = question.Category.ToLower() switch
                    {
                        "type" => targetPokemon.Type,
                        "color" => targetPokemon.Color,
                        "other" => targetPokemon.Other,
                        _ => new Dictionary<string, double>()
                    };

                    var targetValue = attributeDict.TryGetValue(question.TargetAttribute, out double value) ? value : 0.0;

                    var simulatedResponse = targetValue switch
                    {
                        >= 0.8 => UserResponse.Yes,
                        >= 0.6 => UserResponse.Somewhat,
                        >= 0.4 => UserResponse.DontKnow,
                        >= 0.2 => UserResponse.NotReally,
                        _ => UserResponse.No
                    };

                    Console.WriteLine($"Target value: {targetValue} → Response: {simulatedResponse}");

                    game.RecordAnswer(question.Id, simulatedResponse);

                    var topCandidates = game.GetTopCandidates();
                    Console.WriteLine("Top candidates:");
                    foreach (var (pokemon, probability) in topCandidates)
                    {
                        string marker = pokemon.Name == targetPokemon.Name ? " ← Target" : "";
                        Console.WriteLine($"  {pokemon.Name}: {probability:P1}{marker}");
                    }

                    var guess = game.ShouldMakeGuess();
                    if (guess != null)
                    {
                        Console.WriteLine($"\nAI guesses: {guess.Name}");
                        bool correct = guess.Name == targetPokemon.Name;
                        Console.WriteLine(correct ? "Correct" : "Wrong");

                        if (correct)
                        {
                            Console.WriteLine($"Success in {game.QuestionsAsked} questions");
                            return;  // Exit test
                        }
                        else
                        {
                            Console.WriteLine($"Wrong guess recorded");
                            game.RecordWrongGuess(guess);
                        }
                    }


                    // Console.WriteLine($"Questions asked: {game.QuestionsAsked}");
                }
                Console.WriteLine("Test finished");
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