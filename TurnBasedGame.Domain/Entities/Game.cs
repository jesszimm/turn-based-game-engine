using TurnBasedGame.Domain.Exceptions;
using TurnBasedGame.Domain.Interfaces;
using TurnBasedGame.Domain.ValueObjects;

namespace TurnBasedGame.Domain.Entities;

/// <summary>
/// Manages the complete game state including board, players, units, and turn flow.
/// Aggregate root for the entire game session.
/// </summary>
public sealed class Game
{
    private readonly ICombatResolver _combatResolver;
    private readonly GameBoard _board;
    private readonly Player _player1;
    private readonly Player _player2;
    private Player _currentPlayer;
    private int _turnNumber;
    private bool _hasMovedThisTurn;
    private bool _hasAttackedThisTurn;

    /// <summary>
    /// The game board.
    /// </summary>
    public GameBoard Board => _board;

    /// <summary>
    /// Player 1.
    /// </summary>
    public Player Player1 => _player1;

    /// <summary>
    /// Player 2.
    /// </summary>
    public Player Player2 => _player2;

    /// <summary>
    /// The player whose turn it currently is.
    /// </summary>
    public Player CurrentPlayer => _currentPlayer;

    /// <summary>
    /// Current turn number (starts at 1).
    /// </summary>
    public int TurnNumber => _turnNumber;

    /// <summary>
    /// Indicates whether the current player has moved a unit this turn.
    /// </summary>
    public bool HasMovedThisTurn => _hasMovedThisTurn;

    /// <summary>
    /// Indicates whether the current player has attacked this turn.
    /// </summary>
    public bool HasAttackedThisTurn => _hasAttackedThisTurn;

    /// <summary>
    /// Indicates whether the game has ended.
    /// </summary>
    public bool IsGameOver => GetWinner() != null;

    /// <summary>
    /// Creates a new game with a 5x5 board.
    /// </summary>
    /// <param name="player1Name">Name of player 1.</param>
    /// <param name="player2Name">Name of player 2.</param>
    /// <param name="combatResolver">Combat resolver for damage calculation.</param>
    public Game(string player1Name, string player2Name, ICombatResolver combatResolver)
        : this(player1Name, player2Name, combatResolver, new GameBoard(5, 5))
    {
    }

    /// <summary>
    /// Creates a new game with a custom board.
    /// </summary>
    /// <param name="player1Name">Name of player 1.</param>
    /// <param name="player2Name">Name of player 2.</param>
    /// <param name="combatResolver">Combat resolver for damage calculation.</param>
    /// <param name="board">The game board to use.</param>
    public Game(string player1Name, string player2Name, ICombatResolver combatResolver, GameBoard board)
    {
        if (string.IsNullOrWhiteSpace(player1Name))
            throw new ArgumentException("Player 1 name cannot be empty", nameof(player1Name));
        if (string.IsNullOrWhiteSpace(player2Name))
            throw new ArgumentException("Player 2 name cannot be empty", nameof(player2Name));

        _combatResolver = combatResolver ?? throw new ArgumentNullException(nameof(combatResolver));
        _board = board ?? throw new ArgumentNullException(nameof(board));

        _player1 = new Player(player1Name);
        _player2 = new Player(player2Name);
        _currentPlayer = _player1;
        _turnNumber = 1;
        _hasMovedThisTurn = false;
        _hasAttackedThisTurn = false;
    }

    /// <summary>
    /// Gets all units belonging to player 1.
    /// </summary>
    public IEnumerable<Unit> GetPlayer1Units()
    {
        return _board.GetPlayerUnits(_player1.Id).Where(u => u.IsAlive);
    }

    /// <summary>
    /// Gets all units belonging to player 2.
    /// </summary>
    public IEnumerable<Unit> GetPlayer2Units()
    {
        return _board.GetPlayerUnits(_player2.Id).Where(u => u.IsAlive);
    }

    /// <summary>
    /// Gets all units belonging to the current player.
    /// </summary>
    public IEnumerable<Unit> GetCurrentPlayerUnits()
    {
        return _board.GetPlayerUnits(_currentPlayer.Id).Where(u => u.IsAlive);
    }

    /// <summary>
    /// Gets all units belonging to the opponent.
    /// </summary>
    public IEnumerable<Unit> GetOpponentUnits()
    {
        var opponentId = _currentPlayer.Id == _player1.Id ? _player2.Id : _player1.Id;
        return _board.GetPlayerUnits(opponentId).Where(u => u.IsAlive);
    }

    /// <summary>
    /// Places a unit on the board for a specific player.
    /// </summary>
    /// <param name="player">The player who owns the unit.</param>
    /// <param name="unitName">Name of the unit.</param>
    /// <param name="position">Position to place the unit.</param>
    /// <param name="stats">Unit statistics.</param>
    /// <returns>The created unit.</returns>
    public Unit PlaceUnit(Player player, string unitName, Position position, UnitStats stats)
    {
        if (player == null)
            throw new ArgumentNullException(nameof(player));
        if (player.Id != _player1.Id && player.Id != _player2.Id)
            throw new ArgumentException("Player is not part of this game", nameof(player));

        var unit = new Unit(unitName, player.Id, position, stats);
        _board.PlaceUnit(unit, position);
        return unit;
    }

    /// <summary>
    /// Moves a unit to a new position.
    /// Can only move one unit per turn.
    /// </summary>
    /// <param name="unit">The unit to move.</param>
    /// <param name="targetPosition">The destination position.</param>
    /// <exception cref="InvalidMoveException">Thrown if move is invalid or player has already moved.</exception>
    public void MoveUnit(Unit unit, Position targetPosition)
    {
        if (unit == null)
            throw new ArgumentNullException(nameof(unit));

        if (IsGameOver)
            throw new InvalidMoveException("Game is over");

        if (unit.OwnerId != _currentPlayer.Id)
            throw new InvalidMoveException($"Cannot move opponent's unit");

        if (_hasMovedThisTurn)
            throw new InvalidMoveException("Already moved a unit this turn");

        _board.MoveUnit(unit, targetPosition);
        _hasMovedThisTurn = true;
    }

    /// <summary>
    /// Moves a unit to an adjacent tile.
    /// Can only move one unit per turn.
    /// </summary>
    /// <param name="unit">The unit to move.</param>
    /// <param name="targetPosition">The adjacent destination position.</param>
    /// <exception cref="InvalidMoveException">Thrown if move is invalid or player has already moved.</exception>
    public void MoveUnitToAdjacentTile(Unit unit, Position targetPosition)
    {
        if (unit == null)
            throw new ArgumentNullException(nameof(unit));

        if (IsGameOver)
            throw new InvalidMoveException("Game is over");

        if (unit.OwnerId != _currentPlayer.Id)
            throw new InvalidMoveException($"Cannot move opponent's unit");

        if (_hasMovedThisTurn)
            throw new InvalidMoveException("Already moved a unit this turn");

        _board.MoveUnitToAdjacentTile(unit, targetPosition);
        _hasMovedThisTurn = true;
    }

    /// <summary>
    /// Attacks an enemy unit.
    /// Can only attack once per turn.
    /// </summary>
    /// <param name="attacker">The attacking unit.</param>
    /// <param name="defender">The defending unit.</param>
    /// <returns>The amount of damage dealt.</returns>
    /// <exception cref="InvalidCombatException">Thrown if attack is invalid or player has already attacked.</exception>
    public int Attack(Unit attacker, Unit defender)
    {
        if (attacker == null)
            throw new ArgumentNullException(nameof(attacker));
        if (defender == null)
            throw new ArgumentNullException(nameof(defender));

        if (IsGameOver)
            throw new InvalidCombatException("Game is over");

        if (attacker.OwnerId != _currentPlayer.Id)
            throw new InvalidCombatException("Cannot attack with opponent's unit");

        if (_hasAttackedThisTurn)
            throw new InvalidCombatException("Already attacked this turn");

        var damage = _board.Attack(attacker, defender, _combatResolver);
        _hasAttackedThisTurn = true;

        return damage;
    }

    /// <summary>
    /// Ends the current player's turn and switches to the other player.
    /// Resets turn actions and increments turn number.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if game is over.</exception>
    public void EndTurn()
    {
        if (IsGameOver)
            throw new InvalidOperationException("Game is over");

        // Switch player
        _currentPlayer = _currentPlayer.Id == _player1.Id ? _player2 : _player1;

        // Reset turn actions
        _hasMovedThisTurn = false;
        _hasAttackedThisTurn = false;

        // Reset all current player's units
        foreach (var unit in GetCurrentPlayerUnits())
        {
            unit.ResetTurnState();
        }

        // Increment turn number when returning to player 1
        if (_currentPlayer.Id == _player1.Id)
        {
            _turnNumber++;
        }
    }

    /// <summary>
    /// Checks if the current player can perform any more actions this turn.
    /// </summary>
    /// <returns>True if the player can still move or attack; otherwise, false.</returns>
    public bool CanPerformActions()
    {
        return !_hasMovedThisTurn || !_hasAttackedThisTurn;
    }

    /// <summary>
    /// Gets the winner of the game, if any.
    /// </summary>
    /// <returns>The winning player, or null if the game is not over.</returns>
    public Player? GetWinner()
    {
        var player1HasUnits = GetPlayer1Units().Any();
        var player2HasUnits = GetPlayer2Units().Any();

        if (!player1HasUnits && !player2HasUnits)
        {
            // Draw - both players eliminated (shouldn't happen in normal play)
            return null;
        }

        if (!player1HasUnits)
        {
            return _player2;
        }

        if (!player2HasUnits)
        {
            return _player1;
        }

        // Game is still ongoing
        return null;
    }

    /// <summary>
    /// Gets the opponent of the current player.
    /// </summary>
    public Player GetOpponent()
    {
        return _currentPlayer.Id == _player1.Id ? _player2 : _player1;
    }

    /// <summary>
    /// Checks if a specific player is the current player.
    /// </summary>
    public bool IsCurrentPlayer(Player player)
    {
        if (player == null)
            return false;

        return player.Id == _currentPlayer.Id;
    }

    /// <summary>
    /// Gets a summary of the current game state.
    /// </summary>
    public string GetGameStateSummary()
    {
        var player1UnitCount = GetPlayer1Units().Count();
        var player2UnitCount = GetPlayer2Units().Count();

        return $"Turn {_turnNumber}: {_currentPlayer.Name}'s turn | " +
               $"{_player1.Name}: {player1UnitCount} units | " +
               $"{_player2.Name}: {player2UnitCount} units | " +
               $"Moved: {_hasMovedThisTurn} | Attacked: {_hasAttackedThisTurn}";
    }
}