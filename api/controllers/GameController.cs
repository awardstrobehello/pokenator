using Microsoft.AspNetCore.Mvc;
using PokenatorBackend;
using Models;
using System;
using System.Collections.Generic;

namespace api.Controllers
{
    [ApiController]
    [Route("api/game")]
    public class GameController : ControllerBase
    {
        private static Dictionary<Guid, GameState> _gameManager = new();

        [HttpPost]
        public IActionResult StartGame()
        {
            var gameId = Guid.NewGuid();
            var game = GameState.Initialize();

            _gameManager[gameId] = game;

            return Ok(new { gameId });
        }

        [HttpGet("{gameId}/question")]
        public IActionResult GetQuestion(Guid gameId)
        {
            if (!_gameManager.ContainsKey(gameId))
            {
                return NotFound(new { error = "Game not found" });
            }

            var game = _gameManager[gameId];
            var question = game.GetNextQuestion();

            if (question == null)
            {
                return Ok(new
                {
                    question = (object?)null,
                    message = "No more questions available"
                });
            }

            var candidateTuples = game.GetTopCandidates(7);

            var candidates = candidateTuples.Select(tuple => new CandidateDTO
            {
                Pokemon = new PokemonDTO
                {
                    Name = tuple.pokemon.Name
                },
                Confidence = tuple.confidence
            }).ToList();

            return Ok(new
            {
                question = new { question.Id, question.Text },
                candidates = candidates,
                questionsAsked = game.QuestionsAsked
            });
        }

        [HttpPost("{gameId}/answer")]
        public IActionResult SubmitAnswer(Guid gameId, [FromBody] AnswerRequest request)
        {
            if (!_gameManager.ContainsKey(gameId))
            {
                return NotFound(new { error = "Game not found" });
            }

            var game = _gameManager[gameId];

            try
            {
                game.RecordAnswer(request.QuestionId, request.Response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }

            var guess = game.ShouldMakeGuess();
            var candidateTuples = game.GetTopCandidates(7);

            var candidates = candidateTuples.Select(tuple => new CandidateDTO
            {
                Pokemon = new PokemonDTO { Name = tuple.pokemon.Name },
                Confidence = tuple.confidence
            }).ToList();

            bool gameOver = game.QuestionsAsked >= 10 ||
                            candidates.Count == 0;

            return Ok(new
            {
                shouldGuess = guess != null,
                guess = guess != null ? new { guess.Name } : null,
                candidates = candidates,
                questionsAsked = game.QuestionsAsked,
                gameOver = gameOver
            });
        }

        [HttpPost("{gameId}/wrong-guess")]
        public IActionResult RecordWrongGuess(Guid gameId, [FromBody] WrongGuessRequest request)
        {
            if (!_gameManager.ContainsKey(gameId))
            {
                return NotFound(new { error = "Game not found" });
            }

            var game = _gameManager[gameId];
            var pokemon = game.AllPokemon.Find(p => p.Name == request.PokemonName);

            if (pokemon != null)
            {
                game.RecordWrongGuess(pokemon);
            }

            return Ok(new { success = true });
        }
    }

    public class AnswerRequest
    {
        public string QuestionId { get; set; } = string.Empty;
        public UserResponse Response { get; set; }
    }

    public class WrongGuessRequest
    {
        public string PokemonName { get; set; } = string.Empty;
    }

    public class CandidateDTO
    {
        public PokemonDTO Pokemon { get; set; } = new();
        public double Confidence { get; set; }
    }

    public class PokemonDTO
    {
        public string Name { get; set; } = string.Empty;
    }

}