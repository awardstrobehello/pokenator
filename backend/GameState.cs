using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Models;

namespace PokenatorBackend
{
    public class GameState
    {
        private List<Pokemon> _allPokemon;
        private List<Question> _allQuestions;
        private LogicEngine _engine;
        private List<Pokemon> _remainingPokemon;
        private Dictionary<string, UserAnswer> _userAnswers;
        private HashSet<string> _askedQuestions;

        private GameState(List<Pokemon> pokemon, List<Question> questions)
        {
            _allPokemon = pokemon;
            _allQuestions = questions;
            _engine = new LogicEngine();

            _remainingPokemon = new List<Pokemon>(pokemon); 
            _userAnswers = new Dictionary<string, UserAnswer>();
            _askedQuestions = new HashSet<string>();
        }

        public static GameState Initialize()
        {
            var (pokemon, questions) = LoadData();
            return new GameState(pokemon, questions);
        }

        private static (List<Pokemon>, List<Question>) LoadData()
        {
            string pokemonPath = "data/pokemon.json";
            string questionPath = "data/questions.json";

            if (!File.Exists(questionPath) || (!File.Exists(pokemonPath)))
            {
                throw new FileNotFoundException($"File not found");
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

        public Question? GetNextQuestion()
        {
            var availableQuestions = _allQuestions
                .Where(q => !_askedQuestions.Contains(q.Id))
                .ToList();

            var question = _engine.SelectNextQuestion(
                _remainingPokemon,
                availableQuestions,
                _userAnswers);

            if (question != null)
                _askedQuestions.Add(question.Id);

            return question;
        }

        public void RecordAnswer(string questionId, UserResponse response)
        {
            var question = _allQuestions.First(q => q.Id == questionId);

            _userAnswers[questionId] = new UserAnswer
            {
                QuestionId = questionId,
                Category = question.Category,
                TargetAttribute = question.TargetAttribute,
                Response = response
            };
        }
        
        public void RecordWrongGuess(Pokemon pokemon)
        {
            _remainingPokemon.RemoveAll(p => p.Name == pokemon.Name);
        }


        public Pokemon? ShouldMakeGuess()
        {
            return _engine.ShouldMakeGuess(_remainingPokemon, _userAnswers);
        }

        public List<(Pokemon pokemon, double confidence)> GetTopCandidates(int count = 7)
        {
            var probabilities = _engine.GetPokemonProbabilities(_remainingPokemon, _userAnswers);
            return probabilities.Take(count).ToList();
        }

        public int QuestionsAsked => _askedQuestions.Count;

        public List<Pokemon> AllPokemon => _allPokemon;
        public List<Question> AllQuestions => _allQuestions;
        public LogicEngine Engine => _engine;
    }
}