using System;
using System.Collections.Generic;
using System.Linq;
using Models;

namespace PokenatorBackend
{
    public class LogicEngine
    {
        public double CalculateConfidence(Pokemon pokemon, Dictionary<string, UserAnswer> userAnswers)
        {
            if (!userAnswers.Any()) return 1.0;

            double totalConfidence = 1.0;

            foreach (var answer in userAnswers.Values)
            {
                double pokemonValue = GetPokemonAttributeValue(pokemon, answer.Category, answer.TargetAttribute);
                double matchScore = CalculateMatchScore(pokemonValue, answer.Response);

                // Let the information gain algorithm choose important questions
                totalConfidence *= matchScore;
            }

            return Math.Max(totalConfidence, 0.001);
        }
        internal double CalculateMatchScore(double pokemonValue, UserResponse response)
        {
            return response switch
            {
                UserResponse.Yes => pokemonValue,
                UserResponse.No => 1.0 - pokemonValue,

                UserResponse.Somewhat => 0.7 * pokemonValue,
                UserResponse.NotReally => 0.7 * (1.0 - pokemonValue),

                UserResponse.DontKnow => 0.5,

                _ => 0.5
            };
        }
        
        public Question? SelectNextQuestion(List<Pokemon> remainingPokemon, List<Question> availableQuestions,
            Dictionary<string, UserAnswer> userAnswers)
        {
            if (!availableQuestions.Any()) return null; // Should never happen

            var focusedPokemon = GetFocusedPokemon(remainingPokemon, userAnswers);

            // Console.WriteLine($"\n--- Question Selection Debug ---");
            // Console.WriteLine($"Focused on {focusedPokemon.Count} Pokemon (from {remainingPokemon.Count} total)");

            var questionGains = availableQuestions
                .Where(q => !userAnswers.ContainsKey(q.Id))
                .Select(q => new
                {
                    Question = q,
                    Gain = CalculateInformationGain(q, remainingPokemon, userAnswers),
                    Bonus = CategoryBonus(q.Category, userAnswers.Count)
                })
                .Select(x => new
                {
                    x.Question,
                    x.Gain,
                    x.Bonus,
                    AdjustedGain = x.Gain * x.Bonus
                })
                .Where(x => x.AdjustedGain > 0.01)
                .OrderByDescending(x => x.AdjustedGain)
                .ToList();

            // Console.WriteLine($"\nTop 10 question candidates:");
            // foreach (var item in questionGains.Take(5))
            // {
            //     Console.WriteLine($"  [{item.Question.Category}] {item.Question.Text}");
            //     Console.WriteLine($"    Raw Gain: {item.Gain:F4} × Bonus: {item.Bonus:F2} = Adjusted: {item.AdjustedGain:F4}");
            // }

            return questionGains.FirstOrDefault()?.Question;
        }

        private List<Pokemon> GetFocusedPokemon(List<Pokemon> pokemon, Dictionary<string, UserAnswer> userAnswers)
        {
            if (!userAnswers.Any()) return pokemon;

            var rankedPokemon = pokemon
                .Select(p => new { Pokemon = p, Confidence = CalculateConfidence(p, userAnswers) })
                .OrderByDescending(x => x.Confidence)
                .ToList();

            double totalConfidence = rankedPokemon.Sum(x => x.Confidence);

            var probabilities = rankedPokemon
                .Select(x => new
                {
                    x.Pokemon,
                    Probability = x.Confidence / totalConfidence
                })
                .ToList();

            var focusedList = new List<Pokemon>();
            double cumulativeProbability = 0.0;

            foreach (var p in probabilities)
            {
                focusedList.Add(p.Pokemon);
                cumulativeProbability += p.Probability;

                if (cumulativeProbability >= 0.90 && focusedList.Count >= 5)
                    break;
            }

            if (focusedList.Count < 5) focusedList = probabilities.Take(5).Select(x => x.Pokemon).ToList();
            if (focusedList.Count > 10) focusedList = focusedList.Take(10).ToList();

            return focusedList;
        }

        private double CategoryBonus(string category, int questionsAsked)
        {
            // In the early game, strongly prefer factual questions
            if (questionsAsked < 3)
            {
                return category.ToLower() switch
                {
                    "type" => 1.5,      // Types are facts
                    "color" => 1.2,     // Color is pretty objective
                    "other" => 0.7,     // Subjective questions are harder
                    _ => 1.0
                };
            }

            if (questionsAsked < 5)
            {
                return category.ToLower() switch
                {
                    "type" => 1.2,
                    _ => 1.0
                };
            }
            return 1.0;
        }


        public List<(Pokemon Pokemon, double Probability)> GetPokemonProbabilities(List<Pokemon> pokemon, Dictionary<string, UserAnswer> userAnswers)
        {
            var confidences = pokemon
                .Select(p => new { Pokemon = p, Confidence = CalculateConfidence(p, userAnswers) })
                .ToList();

            double totalConfidence = confidences.Sum(x => x.Confidence);

            if (totalConfidence == 0) totalConfidence = 1.0;

            return confidences
                .Select(x => (x.Pokemon, Probability: x.Confidence / totalConfidence))
                .OrderByDescending(x => x.Probability)
                .ToList();
        }




        private double CalculateInformationGain(Question question, List<Pokemon> pokemon, Dictionary<string, UserAnswer> userAnswers)
        {
            if (!pokemon.Any()) return 0; // Realistically should never be called

            // Get distribution of attribute values for question + confidence of pokemon
            var pokemonData = pokemon
                .Select(p => new
                {
                    Pokemon = p,
                    AttributeValue = GetPokemonAttributeValue(p, question.Category, question.TargetAttribute),
                    CurrentConfidence = CalculateConfidence(p, userAnswers)
                })
                .ToList();            // Calculate current entropy

            double currentEntropy = CalculateEntropy(
                pokemonData.Select(pd => pd.AttributeValue).ToList(),
                pokemonData.Select(pd => pd.CurrentConfidence).ToList()
            );

            // Simulate what happens if user gives each possible answer
            double expectedEntropy = 0.0;
            var responses = new[] { UserResponse.Yes, UserResponse.No, UserResponse.Somewhat, UserResponse.NotReally };

            foreach (var response in responses)
            {
                var matchingData = pokemonData
                    .Select(pd => new
                    {
                        pd.AttributeValue,
                        pd.CurrentConfidence,
                        MatchScore = CalculateMatchScore(pd.AttributeValue, response)
                    })
                    .Where(x => x.MatchScore > 0.6)
                    .ToList();

                if (matchingData.Any())
                {
                    double totalConfidence = pokemonData.Sum(pd => pd.CurrentConfidence);
                    double responseWeight = matchingData.Sum(md => md.CurrentConfidence) / totalConfidence;


                    double splitEntropy = CalculateEntropy(
                        matchingData.Select(md => md.AttributeValue).ToList(),
                        matchingData.Select(md => md.CurrentConfidence).ToList()
                    );

                    expectedEntropy += responseWeight * splitEntropy;
                }
            }

            return Math.Max(0, currentEntropy - expectedEntropy);
        }


        private double CalculateEntropy(List<double> values, List<double> weights)
        {
            if (!values.Any()) return 0;

            // Group values into bins (0.0-0.1, 0.1-0.2, etc.)
            var bins = new double[10]; // 10 bins for 0.0 to 1.0
            double totalWeight = 0;


            for (int i = 0; i < values.Count; i++)
            {
                int binIndex = Math.Min(9, (int)(values[i] * 10));
                bins[binIndex] += weights[i];
                totalWeight += weights[i];
            }
            // Calculate entropy using probability of each bin
            double entropy = 0.0;
            for (int i = 0; i < bins.Length; i++)
            {
                if (bins[i] > 0)
                {
                    double probability = bins[i] / totalWeight;
                    entropy -= probability * Math.Log2(probability);
                }
            }

            return entropy;
        }

        private double GetPokemonAttributeValue(Pokemon pokemon, string category, string attribute)
        {
            var attributeDict = category.ToLower() switch
            {
                "type" => pokemon.Type,
                "color" => pokemon.Color,
                "other" => pokemon.Other,
                _ => new Dictionary<string, double>()
            };

            return attributeDict.TryGetValue(attribute, out double value) ? value : 0.0;
        }

        public Pokemon? ShouldMakeGuess(List<Pokemon> remainingPokemon, Dictionary<string, UserAnswer> userAnswers)
        {
            if (!remainingPokemon.Any()) return null;

            // Calculate confidence for all remaining Pokemon
            var probabilities = GetPokemonProbabilities(remainingPokemon, userAnswers);

            var topCandidate = probabilities.First();

            bool highConfidence = topCandidate.Probability >= 0.15;

            bool clearLeader = probabilities.Count == 1 ||  // Only one Pokemon left
                              (probabilities.Count > 1 &&
                               topCandidate.Probability / probabilities[1].Probability >= 2);

            bool tooManyQuestions = userAnswers.Count >= 10;

            if ((highConfidence && clearLeader) || tooManyQuestions)
            {
                return topCandidate.Pokemon;
            }

            return null;
        }
        public List<Pokemon> FilterByConfidence(List<Pokemon> pokemon, Dictionary<string, UserAnswer> userAnswers,
            double threshold = -1) // is default
        {
            if (!userAnswers.Any()) return pokemon;

            // Use default threshold if not specified
            if (threshold < 0) threshold = 0.05;

            return pokemon
                .Where(p => CalculateConfidence(p, userAnswers) > threshold)
                .ToList();
        }
    }
}