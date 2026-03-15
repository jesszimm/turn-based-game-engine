using TurnBasedGame.Application.Commands;
using TurnBasedGame.Domain.Entities;
using TurnBasedGame.Domain.Exceptions;
using TurnBasedGame.Domain.Services;
using TurnBasedGame.Domain.ValueObjects;

namespace TurnBasedGame.Application.Services;

/// <summary>
/// Application service that executes game commands by delegating to domain logic.
/// Acts as a facade between commands and the domain Game class.
/// </summary>
public sealed class GameService
{
    private Game? _game;

    /// <summary>
    /// Gets the current game instance.
    /// </summary>
    public Game? CurrentGame => _game;

    /// <summary>
    /// Checks if a game is currently active.
    /// </summary>
    public bool HasActiveGame => _game != null;

    /// <summary>
    /// Creates a new game with the specified players.
    /// </summary>
    /// <param name="command">Command containing player names and board size.</param>
    /// <returns>Success result or failure with error message.</returns>
    public Result CreateGame(CreateGameCommand command)
    {
        if (command == null)
            return Result.Failure("Command cannot be null");

        try
        {
            var combatResolver = new CombatResolver();
            var board = new GameBoard(command.BoardWidth, command.BoardHeight);

            _game = new Game(
                command.Player1Name,
                command.Player2Name,
                combatResolver,
                board,
                command.ControlTileEnabled);

            return Result.Success();
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to create game: {ex.Message}");
        }
    }

    /// <summary>
    /// Places a unit on the board during game setup.
    /// </summary>
    /// <param name="command">Command containing unit details and position.</param>
    /// <returns>Success result with unit ID, or failure with error message.</returns>
    public Result<Guid> PlaceUnit(PlaceUnitCommand command)
    {
        if (command == null)
            return Result<Guid>.Failure("Command cannot be null");

        if (_game == null)
            return Result<Guid>.Failure("No active game");

        try
        {
            var player = GetPlayer(command.PlayerId);
            if (player == null)
                return Result<Guid>.Failure("Player not found");

            var position = new Position(command.X, command.Y);
            var stats = new UnitStats(
                command.MaxHealth,
                command.AttackPower,
                0,
                command.MovementRange);

            var unit = _game.PlaceUnit(player, command.UnitName, position, stats);

            return Result<Guid>.Success(unit.Id);
        }
        catch (InvalidMoveException ex)
        {
            return Result<Guid>.Failure(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            return Result<Guid>.Failure($"Failed to place unit: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes a move unit command.
    /// Finds the unit by ID and moves it to the target position.
    /// </summary>
    /// <param name="command">Command containing unit ID and target coordinates.</param>
    /// <returns>Success result or failure with error message.</returns>
    public Result MoveUnit(MoveUnitCommand command)
    {
        if (command == null)
            return Result.Failure("Command cannot be null");

        if (_game == null)
            return Result.Failure("No active game");

        try
        {
            var unit = _game.Board.FindUnit(command.UnitId);
            if (unit == null)
                return Result.Failure($"Unit with ID {command.UnitId} not found");

            var targetPosition = new Position(command.TargetX, command.TargetY);

            _game.MoveUnit(unit, targetPosition);

            return Result.Success();
        }
        catch (InvalidMoveException ex)
        {
            return Result.Failure(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to move unit: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes an attack unit command.
    /// Finds both units by ID and executes the attack.
    /// </summary>
    /// <param name="command">Command containing attacker and defender IDs.</param>
    /// <returns>Success result with damage dealt, or failure with error message.</returns>
    public Result<int> AttackUnit(AttackUnitCommand command)
    {
        if (command == null)
            return Result<int>.Failure("Command cannot be null");

        if (_game == null)
            return Result<int>.Failure("No active game");

        try
        {
            var attacker = _game.Board.FindUnit(command.AttackerId);
            if (attacker == null)
                return Result<int>.Failure($"Attacker with ID {command.AttackerId} not found");

            var defender = _game.Board.FindUnit(command.DefenderId);
            if (defender == null)
                return Result<int>.Failure($"Defender with ID {command.DefenderId} not found");

            var damage = _game.Attack(attacker, defender);

            return Result<int>.Success(damage);
        }
        catch (InvalidCombatException ex)
        {
            return Result<int>.Failure(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return Result<int>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            return Result<int>.Failure($"Failed to execute attack: {ex.Message}");
        }
    }

    /// <summary>
    /// Ends the current player's turn.
    /// </summary>
    /// <param name="command">End turn command.</param>
    /// <returns>Success result or failure with error message.</returns>
    public Result EndTurn(EndTurnCommand command)
    {
        if (command == null)
            return Result.Failure("Command cannot be null");

        if (_game == null)
            return Result.Failure("No active game");

        try
        {
            _game.EndTurn();
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to end turn: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets a player by ID from the current game.
    /// </summary>
    private Player? GetPlayer(Guid playerId)
    {
        if (_game == null)
            return null;

        if (_game.Player1.Id == playerId)
            return _game.Player1;

        if (_game.Player2.Id == playerId)
            return _game.Player2;

        return null;
    }

    /// <summary>
    /// Checks if the game is over.
    /// </summary>
    public bool IsGameOver()
    {
        return _game?.IsGameOver ?? false;
    }

    /// <summary>
    /// Gets the winner of the game, if any.
    /// </summary>
    public Player? GetWinner()
    {
        return _game?.GetWinner();
    }

    /// <summary>
    /// Gets the current player whose turn it is.
    /// </summary>
    public Player? GetCurrentPlayer()
    {
        return _game?.CurrentPlayer;
    }

    /// <summary>
    /// Gets all units for the current player.
    /// </summary>
    public IEnumerable<Unit> GetCurrentPlayerUnits()
    {
        return _game?.GetCurrentPlayerUnits() ?? Enumerable.Empty<Unit>();
    }

    /// <summary>
    /// Gets all units for the opponent.
    /// </summary>
    public IEnumerable<Unit> GetOpponentUnits()
    {
        return _game?.GetOpponentUnits() ?? Enumerable.Empty<Unit>();
    }

    /// <summary>
    /// Checks if the current player can still perform actions this turn.
    /// </summary>
    public bool CanPerformActions()
    {
        return _game?.CanPerformActions() ?? false;
    }
}
