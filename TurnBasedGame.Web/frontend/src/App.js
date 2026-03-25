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
  const hasInitialized = useRef(false);

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
    if (hasInitialized.current) return;
    hasInitialized.current = true;
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
      <h1 className="PageTitle">Tactical Squares</h1>
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
          <div className="GameLayout">
            <div className="Rules">
              <h2>How to Play</h2>
              <ul>
                <li>On your turn, click one of your units to select it.</li>
                <li>Blue-highlighted tiles show where the selected unit can move.</li>
                <li>Click an empty highlighted tile to move, then your turn ends.</li>
                <li>To attack, click an enemy unit that is adjacent (including diagonals).</li>
                <li>Each unit can either move or attack during a turn.</li>
                <li>When all units for one side are defeated, the other side wins.</li>
              </ul>
            </div>
            <div className="BoardColumn">
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
            </div>
            <div className="Legend">
              <h2>{playerName || PLAYER_1} vs {aiName || 'CPU'}</h2>
              <p className="LegendKey">W = Warrior, S = Scout</p>
              <ul className="LegendList">
                {liveUnits
                  .filter((unit) => unit.owner === PLAYER_1)
                  .map((unit) => (
                    <li key={unit.id} className="LegendItem">
                      <div className="LegendTitle">
                        <span className="LegendOwner">{playerName || PLAYER_1}</span>
                        <span className="LegendUnit">{unit.name}</span>
                      </div>
                      <div className="LegendStats">
                        <span className="StatPill">HP {unit.health}</span>
                        <span className="StatPill">ATK {unit.attackPower}</span>
                        <span className="StatPill">MOVE {unit.movementRange}</span>
                      </div>
                    </li>
                  ))}
                {liveUnits
                  .filter((unit) => unit.owner === PLAYER_2)
                  .map((unit) => (
                    <li key={unit.id} className="LegendItem LegendItem-ai">
                      <div className="LegendTitle">
                        <span className="LegendOwner">{aiName || 'CPU'}</span>
                        <span className="LegendUnit">{unit.name}</span>
                      </div>
                      <div className="LegendStats">
                        <span className="StatPill">HP {unit.health}</span>
                        <span className="StatPill">ATK {unit.attackPower}</span>
                        <span className="StatPill">MOVE {unit.movementRange}</span>
                      </div>
                    </li>
                  ))}
              </ul>
            </div>
          </div>
        </>
      )}
    </div>
  );
}

export default App;
