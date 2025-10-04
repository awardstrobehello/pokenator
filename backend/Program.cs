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

            bool debug = args.Contains("--debug");

            if (debug) Console.WriteLine("debug mode activated: Showing confidence calculations\n");
            // RandomTest is a random pokemon and the akinator guesses random questions

            if (debug || args.Length == 0) RealGame(debug);
            else RandomTest();

        }

        static void RealGame(bool debug = false)
        {
            Console.WriteLine("RealGame called");
            try
            {
                var game = GameState.Initialize();
                Console.WriteLine($"Think of a Pokemon!\n");

                for (int round = 1; round <= 10; round++)
                {

                    var question = game.GetNextQuestion();
                    if (question == null)
                    {
                        Console.WriteLine("No more questions left...");
                        break;
                    }

                    Console.WriteLine($"\nRound {round}: {question.Text}");
                    Console.WriteLine("1 Yes      2 Somewhat      3 NotReally      4 No      5 DontKnow");
                    string input = Console.ReadLine()?.Trim().ToLower() ?? "";


                    int validInput = -1;
                    while (validInput == -1) {
                        validInput = 1;
                        switch (input)
                        {
                            case "yes" or "y" or "1":
                                game.RecordAnswer(question.Id, UserResponse.Yes);
                                break;
                            case "somewhat" or "s" or "2":
                                game.RecordAnswer(question.Id, UserResponse.Somewhat);
                                break;
                            case "notreally" or "nr" or "3":
                                game.RecordAnswer(question.Id, UserResponse.NotReally);
                                break;
                            case "no" or "n" or "4":
                                game.RecordAnswer(question.Id, UserResponse.No);
                                break;
                            case "dontknow" or "idk" or "5":
                                game.RecordAnswer(question.Id, UserResponse.DontKnow);
                                break;
                            default:
                                Console.WriteLine("Incorrect input");
                                validInput = -1;
                                input = Console.ReadLine()?.Trim().ToLower() ?? "";
                                break;
                        }
                    }

                    Console.WriteLine($"Response: {input}");


                    if (debug)
                    {
                        var topCandidates = game.GetTopCandidates();
                        Console.WriteLine("Top candidates:");
                        foreach (var (pokemon, probability) in topCandidates)
                        {
                            Console.WriteLine($"  {pokemon.Name}: {probability:P1}");
                        }
                        
                    }


                    var guess = game.ShouldMakeGuess();
                    if (guess != null)
                    {
                        Console.WriteLine($"\nIs it {guess.Name}? (y/n)");
                        var correct = Console.ReadLine()?.Trim().ToLower() == "y";
                        if (correct)
                        {
                            Console.WriteLine($"\nI guessed it in {game.QuestionsAsked} questions!");
                            return;
                        }
                        else
                        {
                            Console.WriteLine("I see... I've written that down.");
                            game.RecordWrongGuess(guess);
                        }
                    }

                }
                Console.WriteLine("Game complete.\n");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
            }
        }

        static (List<Pokemon>, List<Question>) LoadData()
        {
            string pokemonPath = "data/pokemon.json";
            string questionPath = "data/questions.json";

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

                for (int round = 1; round <= 10; round++)
                {
                    Console.WriteLine($"\nRound {round}");


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

                        if (correct)
                        {
                            Console.WriteLine($"Correct! Success in {game.QuestionsAsked} questions.\n");
                            return;  // Exit test
                        }
                        else
                        {
                            Console.WriteLine($"Incorrect. Wrong guess recorded\n");
                            game.RecordWrongGuess(guess);
                        }
                    }


                    // Console.WriteLine($"Questions asked: {game.QuestionsAsked}");
                }
                Console.WriteLine("Game complete.\n");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
            }

        }


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