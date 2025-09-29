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

            var questionGains = availableQuestions
                .Where(q => !userAnswers.ContainsKey(q.Id))
                .Select(q => new
                {
                    Question = q,
                    Gain = CalculateInformationGain(q, remainingPokemon, userAnswers),
                })
                .Where(x => x.Gain > 0.01)
                .OrderByDescending(x => x.Gain)
                .ToList();

            return questionGains.FirstOrDefault()?.Question;
        }

        private double GetCategoryBonus(string category, int questionsAsked)
        {
            // Early game: Prefer high-discrimination categories
            // Late game: All categories equal
            if (questionsAsked < 3)
            {
                return category.ToLower() switch
                {
                    "type" => 1.2,      // Types discriminate well early
                    "other" => 1.1,     // Stronger
                    "color" => 1.0,     // Okay. Pokemon tend to have multiple colors
                    _ => 1.0
                };
            }
            return 1.0; // Late game: trust pure information gain
        }

        // Where are the calculations for the pokemon attribute values per questions being asked?
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
            var confidences = remainingPokemon
                .Select(p => new { Pokemon = p, Confidence = CalculateConfidence(p, userAnswers) })
                .OrderByDescending(x => x.Confidence)
                .ToList();

            var topCandidate = confidences.First();

            // Dynamic threshold: start low, increase as we ask more questions
            double dynamicThreshold = Math.Min(0.8, 0.55 + (userAnswers.Count * 0.04));

            // Make guess if:
            // 1. High confidence in top candidate OR
            // 2. Only a few Pokemon left OR  
            // 3. Top candidate significantly higher than others

            double confidenceThreshold = topCandidate.Confidence * 0.9;
            int closeContenders = confidences.Count(c => c.Confidence >= confidenceThreshold);


            bool highConfidenceWithClearWinner = topCandidate.Confidence > dynamicThreshold && closeContenders <= 2;
            bool fewRemaining = remainingPokemon.Count <= 2;
            bool significantGap = confidences.Count > 1 &&
                                 topCandidate.Confidence / confidences[1].Confidence > 1.5 &&
                                 closeContenders <= 3;


            if (highConfidenceWithClearWinner || fewRemaining || significantGap)
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