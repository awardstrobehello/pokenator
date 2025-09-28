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
            if (!userAnswers.Any()) return 1.0; // No answers yet, all equally likely

            double totalConfidence = 1.0;

            foreach (var answer in userAnswers.Values)
            {
                double pokemonValue = GetPokemonAttributeValue(pokemon, answer.Category, answer.TargetAttribute);
                double matchScore = CalculateMatchScore(pokemonValue, answer.Response);

                // Weight by question importance and apply to total confidence
                double weightedScore = Math.Pow(matchScore, answer.Importance);
                totalConfidence *= weightedScore;
            }

            // Prevent confidence from getting too close to 0
            return Math.Max(totalConfidence, 0.001);
        }
        internal double CalculateMatchScore(double pokemonValue, UserResponse response)
        {
            return response switch
            {
                UserResponse.Yes => pokemonValue,
                UserResponse.No => 1.0 - pokemonValue,

                UserResponse.Probably => 0.7 * pokemonValue,           
                UserResponse.ProbablyNot => 0.7 * (1.0 - pokemonValue),

                UserResponse.DontKnow => 0.5,

                _ => 0.5
            };
        }
        public Question? SelectNextQuestion(List<Pokemon> remainingPokemon, List<Question> availableQuestions,
            Dictionary<string, UserAnswer> userAnswers)
        {
            if (!availableQuestions.Any()) return null;

            var questionGains = availableQuestions
                .Where(q => !userAnswers.ContainsKey(q.Id))
                .Select(q => new { Question = q, Gain = CalculateInformationGain(q, remainingPokemon) })
                .Where(x => x.Gain > 0.01)
                .OrderByDescending(x => x.Gain * x.Question.Importance)
                .ToList();

            return questionGains.FirstOrDefault()?.Question;
        }
        
        private double CalculateInformationGain(Question question, List<Pokemon> pokemon)
        {
            if (!pokemon.Any()) return 0;

            // Get distribution of attribute values for this question
            var values = pokemon
                .Select(p => GetPokemonAttributeValue(p, question.Category, question.TargetAttribute))
                .ToList();

            // Calculate current entropy (how mixed up the values are)
            double currentEntropy = CalculateEntropy(values);

            // Simulate what happens if user gives each possible answer
            double expectedEntropy = 0.0;
            var responses = new[] { UserResponse.Yes, UserResponse.No, UserResponse.Probably, UserResponse.ProbablyNot };

            foreach (var response in responses)
            {
                // Find Pokemon that would get good match scores for this response
                var matchingPokemon = new List<double>();

                foreach (var value in values)
                {
                    double matchScore = CalculateMatchScore(value, response);
                    if (matchScore > 0.6) // Threshold for "good match"
                    {
                        matchingPokemon.Add(value);
                    }
                }

                if (matchingPokemon.Any())
                {
                    // Weight by how likely this response is
                    double responseProbability = (double)matchingPokemon.Count / values.Count;
                    double splitEntropy = CalculateEntropy(matchingPokemon);
                    expectedEntropy += responseProbability * splitEntropy;
                }
            }

            return Math.Max(0, currentEntropy - expectedEntropy);
        }


        private double CalculateEntropy(List<double> values)
        {
            if (!values.Any()) return 0;

            // Group values into bins (0.0-0.1, 0.1-0.2, etc.)
            var bins = new int[10]; // 10 bins for 0.0 to 1.0

            foreach (var value in values)
            {
                int binIndex = Math.Min(9, (int)(value * 10)); // Clamp to 0-9
                bins[binIndex]++;
            }

            // Calculate entropy using probability of each bin
            double entropy = 0.0;
            int totalCount = values.Count;

            for (int i = 0; i < bins.Length; i++)
            {
                if (bins[i] > 0)
                {
                    double probability = (double)bins[i] / totalCount;
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
            double dynamicThreshold = Math.Min(0.8,
                0.55 + (userAnswers.Count * 0.04));

            // Make guess if:
            // 1. High confidence in top candidate OR
            // 2. Only a few Pokemon left OR  
            // 3. Top candidate significantly higher than others

            // Need to fix if there's a lot of potential "correct" pokemon

            if (topCandidate.Confidence > dynamicThreshold ||
                remainingPokemon.Count <= 3 ||
                (confidences.Count > 1 && topCandidate.Confidence / confidences[1].Confidence > 2.0))
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

    public class UserAnswer
    {
        public string QuestionId { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string TargetAttribute { get; set; } = string.Empty;
        public UserResponse Response { get; set; }
        public double Certainty { get; set; } = 0.8; // Default moderate certainty
        public double Importance { get; set; } = 1.0;
    }

    public enum UserResponse
    {
        Yes,
        No,
        Probably,
        ProbablyNot,
        DontKnow
    }
}