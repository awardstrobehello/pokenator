import { UserResponse } from './types/game.types';
import type { Question, Candidate } from './types/game.types';
import { useState } from 'react';

function App() {
  const [gameId, setGameId] = useState<string | null>(null);
  const [currentQuestion, setCurrentQuestion] = useState<Question | null>(null);
  const [candidates, setCandidates] = useState<Candidate[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [gameStarted, setGameStarted] = useState(false);
  const [questionsAsked, setQuestionsAsked] = useState(1);


  const [currentGuess, setCurrentGuess] = useState<string | null>(null);
  const [showGuessModal, setShowGuessModal] = useState(false);
  const [gameWon, setGameWon] = useState(false);
  const [gameLost, setGameLost] = useState(false);



  const startGame = async () => {
    if (gameStarted) return;
    setIsLoading(true);
    try {
      const response = await fetch('/api/game', { method: 'POST' });
      if (response.ok) {
        const data = await response.json();
        // console.log('Game started:', data.gameId);
        setGameId(data.gameId);
        setGameStarted(true);

        await getQuestion(data.gameId);
      }
    } catch (error) {
      console.error('Error:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const getQuestion = async (id: string) => {
    setIsLoading(true);
    try {
      const response = await fetch(`/api/game/${id}/question`);
      if (response.ok) {
        const data = await response.json();

        // console.log('Full API response:', data);
        // console.log('Question:', data.question);
        // console.log('Candidates array:', data.candidates);
        // console.log('First candidate:', data.candidates[0]);
        // console.log('First candidate keys:', Object.keys(data.candidates[0]));

        setCurrentQuestion(data.question);
        setCandidates(data.candidates);
        setQuestionsAsked(data.questionsAsked || 0);
      }
    } catch (error) {
      console.error('Error getting question:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const responseMap = {
    'Yes': 0,
    'Somewhat': 1,
    'NotReally': 2,
    'No': 3,
    'DontKnow': 4
  };

  const handleAnswer = async (response: UserResponse) => {
    if (!gameId || !currentQuestion) return;

    // console.log('Request body:', JSON.stringify({
    //   questionId: currentQuestion.id,
    //   response: response
    // }));


    setIsLoading(true);
    try {
      const res = await fetch(`/api/game/${gameId}/answer`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },


        body: JSON.stringify({
          questionId: currentQuestion.id,
          response: responseMap[response]
        })
      });
      if (res.ok) {
        const data = await res.json();
        // console.log('Answer:', data);
        setCandidates(data.candidates);
        setQuestionsAsked(data.questionsAsked || questionsAsked + 1);

        if (data.gameOver) {
          setGameLost(true);
          return; 
        }

        if (data.shouldGuess) {
          setCurrentGuess(data.guess.name);
          setShowGuessModal(true);
          return;
        }

        await getQuestion(gameId);
      }

    } catch (error) {
      console.error('Error submitting answer:', error);
    } finally {
      setIsLoading(false);
    }
  };


  return (
    <div style={{
      maxWidth: '600px', padding: '20px', textAlign: 'center', 
      display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '20px' }}>
      <h1>Pokenator</h1>

      {!gameStarted ? (
        <button onClick={startGame} disabled={isLoading}>
          {isLoading ? 'Starting...' : 'Start Game'}
        </button>
      ) : (
        <>
          {!gameWon && !gameLost && currentQuestion && (
            <>
              <p>Question {questionsAsked}</p>
              <h2>{currentQuestion.text}</h2>

                <div style={{
                  display: 'flex', gap: '10px', flexWrap: 'wrap', justifyContent: 'center', marginBottom: '20px' }}>
                <button onClick={() => handleAnswer(UserResponse.Yes)} disabled={isLoading}>
                  Yes
                </button>
                <button onClick={() => handleAnswer(UserResponse.No)} disabled={isLoading}>
                  No
                </button>
                <button onClick={() => handleAnswer(UserResponse.Somewhat)} disabled={isLoading}>
                  Somewhat
                </button>
                <button onClick={() => handleAnswer(UserResponse.NotReally)} disabled={isLoading}>
                  Not Really
                </button>
                <button onClick={() => handleAnswer(UserResponse.DontKnow)} disabled={isLoading}>
                  Don't Know
                </button>
              </div>
            </>
          )}
          {!gameWon && !gameLost && candidates.length > 0 && (
            <>
              <h3>Top Candidates:</h3>
              {candidates.map((candidate) => (
                <div key={candidate.pokemon.name}>
                  <p>
                    {candidate.pokemon.name}: {(candidate.confidence * 100).toFixed(2)}%
                  </p>
                </div>
              ))}
            </>
          )}
        </>
      )}
      {showGuessModal && currentGuess && (
        <div style={{
          position: 'fixed',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          backgroundColor: 'rgba(0,0,0,0.7)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          zIndex: 1000
        }}>
          <div style={{
            backgroundColor: 'white',
            color: 'black',
            padding: '40px',
            borderRadius: '10px',
            textAlign: 'center',
            boxShadow: '0 4px 6px rgba(0,0,0,0.3)'
          }}>
            <h2 style={{ marginBottom: '30px' }}>Is it {currentGuess}?</h2>
            <div style={{ display: 'flex', gap: '20px', justifyContent: 'center' }}>
              <button
                onClick={() => {
                  setGameWon(true);
                  setShowGuessModal(false);
                }}
                style={{ padding: '12px 30px', fontSize: '16px', cursor: 'pointer' }}
              >
                Yes!
              </button>
              <button
                onClick={async () => {
                  setShowGuessModal(false);

                  await fetch(`/api/game/${gameId}/wrong-guess`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ pokemonName: currentGuess })
                  });

                  setCurrentGuess(null);
                  await getQuestion(gameId!);
                }}
                style={{ padding: '12px 30px', fontSize: '16px', cursor: 'pointer' }}
              >
                No
              </button>
            </div>
          </div>
        </div>
      )}

      {gameWon && (
        <div style={{ textAlign: 'center', marginTop: '50px' }}>
          <h2>I guessed it in {questionsAsked} questions!</h2>
          <p style={{ fontSize: '20px', marginTop: '20px' }}>It was {currentGuess}!</p>
          <button
            onClick={() => window.location.reload()}
            style={{ marginTop: '30px', padding: '12px 40px', fontSize: '16px', cursor: 'pointer' }}
          >
            Play Again
          </button>
        </div>
      )}

      {gameLost && (
        <div style={{ textAlign: 'center', marginTop: '50px' }}>
          <h2>I'm stumped!</h2>
          <p style={{ fontSize: '18px', marginTop: '20px' }}>
            I couldn't guess your Pokémon after {questionsAsked} questions.
          </p>
          <button
            onClick={() => window.location.reload()}
            style={{ marginTop: '30px', padding: '12px 40px', fontSize: '16px', cursor: 'pointer' }}
          >
            Play Again
          </button>
        </div>
      )}
    </div>
  );
}

export default App;