import './App.css';
import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { attackUnit, createGame, getGame, moveUnit } from './api/gameApi';
import { getValidMoves } from './utils/board';

function App() {
  const PLAYER_1 = 'Player 1';
  const PLAYER_2 = 'Player 2';
  const [gameId, setGameId] = useState(null);
  const [gameState, setGameState] = useState(null);
  const [error, setError] = useState(null);
  const [moveError, setMoveError] = useState(null);
  const [selectedUnitId, setSelectedUnitId] = useState(null);
  const [isLoadingGame, setIsLoadingGame] = useState(false);
  const [playerName, setPlayerName] = useState(PLAYER_1);
  const [aiName, setAiName] = useState('CPU');
  const boardSize = 5;
  const gameStateRef = useRef(gameState);

  const fetchGameState = useCallback(async (id) => {
    const stateResponse = await getGame(id);
    if (!stateResponse.ok) {
      throw new Error(`Fetch failed: ${stateResponse.status}`);
    }
    const stateData = await stateResponse.json();
    setGameState(stateData);
  }, []);

  const startNewGame = useCallback(async () => {
    setIsLoadingGame(true);
    setGameState(null);
    setSelectedUnitId(null);
    setMoveError(null);
    const createResponse = await createGame();

    if (!createResponse.ok) {
      throw new Error(`Create failed: ${createResponse.status}`);
    }

    const createData = await createResponse.json();
    setGameId(createData.gameId);
    await fetchGameState(createData.gameId);
    setIsLoadingGame(false);
  }, [fetchGameState]);

  useEffect(() => {
    gameStateRef.current = gameState;
  }, [gameState]);

  useEffect(() => {
    let isActive = true;

    async function loadGame() {
      try {
        await startNewGame();
      } catch (err) {
        if (!isActive) return;
        setError(err.message ?? 'Unexpected error');
      }
    }

    loadGame();

    return () => {
      isActive = false;
    };
  }, [startNewGame]);

  async function handleCellClick(x, y, unitAtCell) {
    if (!gameId || !gameState || isLoadingGame || isGameOver) return;

    if (unitAtCell) {
      if (gameState.currentPlayer !== PLAYER_1) return;

      if (unitAtCell.owner === PLAYER_1) {
        setMoveError(null);
        setSelectedUnitId(unitAtCell.id);
        return;
      }

      if (selectedUnitId) {
        const currentUnits = gameStateRef.current?.units ?? [];
        const attackerExists = currentUnits.some((unit) => unit.id === selectedUnitId);
        const targetExists = currentUnits.some((unit) => unit.id === unitAtCell.id);
        if (!attackerExists || !targetExists) {
          setSelectedUnitId(null);
          setMoveError('Selected unit is no longer available.');
          return;
        }

        try {
          const response = await attackUnit(gameId, {
            attackerId: selectedUnitId,
            targetId: unitAtCell.id,
          });

          if (response.status === 404) {
            setMoveError('Game session not found. Please start a new game.');
            return;
          }

          if (!response.ok) {
            const message = await response.text();
            throw new Error(message || `Attack failed: ${response.status}`);
          }

          await response.json();
          await fetchGameState(gameId);
          setSelectedUnitId(null);
          setMoveError(null);
        } catch (err) {
          setMoveError(err.message ?? 'Attack failed');
        }
      }

      return;
    }

    if (!selectedUnitId) return;
    const selectedStillExists = (gameStateRef.current?.units ?? [])
      .some((unit) => unit.id === selectedUnitId);
    if (!selectedStillExists) {
      setSelectedUnitId(null);
      setMoveError('Selected unit is no longer available.');
      return;
    }

    try {
      const response = await moveUnit(gameId, { unitId: selectedUnitId, x, y });

      if (response.status === 404) {
        setMoveError('Game session not found. Please start a new game.');
        return;
      }

      if (!response.ok) {
        const message = await response.text();
        throw new Error(message || `Move failed: ${response.status}`);
      }

      await response.json();
      await fetchGameState(gameId);
      setSelectedUnitId(null);
      setMoveError(null);
    } catch (err) {
      setMoveError(err.message ?? 'Move failed');
    }
  }

  const units = gameState?.units ?? [];
  const liveUnits = units.filter((unit) => unit.health > 0);
  const hasPlayerUnits = liveUnits.some((unit) => unit.owner === PLAYER_1);
  const hasAiUnits = liveUnits.some((unit) => unit.owner === PLAYER_2);
  const isGameOver = Boolean(gameState) && (!hasPlayerUnits || !hasAiUnits);
  const winnerLabel = hasPlayerUnits ? (playerName || PLAYER_1) : (aiName || 'CPU');

  const displayCurrentPlayer = gameState?.currentPlayer === PLAYER_1
    ? (playerName || PLAYER_1)
    : (aiName || 'CPU');

  const selectedUnit = liveUnits.find((unit) => unit.id === selectedUnitId);
  const validMoveSet = useMemo(() => {
    if (!selectedUnit || gameState?.currentPlayer !== PLAYER_1) return new Set();
    return getValidMoves(selectedUnit, liveUnits, boardSize);
  }, [selectedUnit, liveUnits, boardSize, gameState, PLAYER_1]);

  if (error) {
    return (
      <div className="App">
        <h1>TurnBasedGame</h1>
        <p>Error: {error}</p>
      </div>
    );
  }

  async function handleNewGameClick() {
    try {
      await startNewGame();
    } catch (err) {
      setIsLoadingGame(false);
      setError(err.message ?? 'Unexpected error');
    }
  }

  return (
    <div className="App">
      <h1>TurnBasedGame</h1>
      <div className="NameRow">
        <label className="NameField">
          Your name
          <input
            type="text"
            value={playerName}
            onChange={(event) => setPlayerName(event.target.value)}
            placeholder={PLAYER_1}
          />
        </label>
        <label className="NameField">
          AI name
          <input
            type="text"
            value={aiName}
            onChange={(event) => setAiName(event.target.value)}
            placeholder="CPU"
          />
        </label>
      </div>
      <button type="button" className="NewGameButton" onClick={handleNewGameClick}>
        New Game
      </button>
      <p className="SelectedUnit">
        Selected Unit: {selectedUnitId ? 'Selected' : 'None'}
      </p>
      {gameState && (
        <p className="CurrentPlayer">
          Current Player: {displayCurrentPlayer}
        </p>
      )}
      {isGameOver && (
        <div className="Banner Banner-gameover">
          {winnerLabel} wins
        </div>
      )}
      {moveError && <div className="Banner Banner-error">{moveError}</div>}
      {!gameState && <p>Loading game state...</p>}
      {gameState && (
        <>
          <div className="BoardWrapper">
            {isLoadingGame && <div className="BoardOverlay">Loading...</div>}
            <div className="Board" role="grid" aria-label="Game board">
            {Array.from({ length: boardSize * boardSize }, (_, index) => {
              const x = index % boardSize;
              const y = Math.floor(index / boardSize);
              const unit = liveUnits.find((u) => u.x === x && u.y === y);
              const isValidMove = validMoveSet.has(`${x},${y}`);
              const isPlayerUnit = unit?.owner === PLAYER_1;
              return (
                <button
                  type="button"
                  key={`${x}-${y}`}
                  className={`Cell ${isValidMove ? 'Cell-valid' : ''}`}
                  role="gridcell"
                  onClick={() => handleCellClick(x, y, unit)}
                  disabled={isGameOver}
                >
                  {unit ? (
                    <div className={`Unit ${isPlayerUnit ? 'Unit-player' : 'Unit-ai'} ${unit.id === selectedUnitId ? 'Unit-selected' : ''}`}>
                      <div className="Unit-label">{unit.name ? unit.name[0].toUpperCase() : 'U'}</div>
                      <div className="Unit-hp">HP {unit.health}</div>
                    </div>
                  ) : (
                    <div className="Cell-empty" />
                  )}
                </button>
              );
            })}
            </div>
          </div>
          <div className="Legend">
            <h2>{playerName || PLAYER_1} vs {aiName || 'CPU'}</h2>
            <p className="LegendKey">W = Warrior, S = Scout</p>
            <ul>
              {liveUnits
                .filter((unit) => unit.owner === PLAYER_1)
                .map((unit, index) => (
                  <li key={unit.id}>
                    {playerName || PLAYER_1} {unit.name} {index + 1}: HP {unit.health} ATK {unit.attackPower} MOVE {unit.movementRange}
                  </li>
                ))}
              {liveUnits
                .filter((unit) => unit.owner === PLAYER_2)
                .map((unit, index) => (
                  <li key={unit.id}>
                    {aiName || 'CPU'} {unit.name} {index + 1}: HP {unit.health} ATK {unit.attackPower} MOVE {unit.movementRange}
                  </li>
                ))}
            </ul>
          </div>
        </>
      )}
    </div>
  );
}

export default App;
