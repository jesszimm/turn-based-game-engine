import './App.css';
import { useEffect, useState } from 'react';

function App() {
  const [gameId, setGameId] = useState(null);
  const [gameState, setGameState] = useState(null);
  const [error, setError] = useState(null);

  useEffect(() => {
    let isActive = true;

    async function loadGame() {
      try {
        const createResponse = await fetch('http://localhost:5187/api/game/create', {
          method: 'POST',
        });

        if (!createResponse.ok) {
          throw new Error(`Create failed: ${createResponse.status}`);
        }

        const createData = await createResponse.json();
        if (!isActive) return;
        setGameId(createData.gameId);

        const stateResponse = await fetch(`http://localhost:5187/api/game/${createData.gameId}`);
        if (!stateResponse.ok) {
          throw new Error(`Fetch failed: ${stateResponse.status}`);
        }

        const stateData = await stateResponse.json();
        if (!isActive) return;
        setGameState(stateData);
      } catch (err) {
        if (!isActive) return;
        setError(err.message ?? 'Unexpected error');
      }
    }

    loadGame();

    return () => {
      isActive = false;
    };
  }, []);

  if (error) {
    return (
      <div className="App">
        <h1>TurnBasedGame</h1>
        <p>Error: {error}</p>
      </div>
    );
  }

  return (
    <div className="App">
      <h1>TurnBasedGame</h1>
      <p>Game ID: {gameId ?? 'Loading...'}</p>
      <h2>Units</h2>
      {!gameState && <p>Loading game state...</p>}
      {gameState && (
        <ul>
          {gameState.units.map((unit) => (
            <li key={unit.id}>
              {unit.id}: ({unit.x}, {unit.y}) HP {unit.health}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

export default App;
