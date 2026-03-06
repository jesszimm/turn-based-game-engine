using TurnBasedGame.Application.Commands;
using TurnBasedGame.Application.Interfaces;
using TurnBasedGame.Application.Queries;
using TurnBasedGame.ConsoleUI.Renderers;

namespace TurnBasedGame.ConsoleUI.InputHandlers;

/// <summary>
/// Handles all user input from the console.
/// Parses commands, validates input, and executes application operations.
/// </summary>
public sealed class ConsoleInputHandler
{
    private readonly IGameEngine _gameEngine;
    private readonly IBoardRenderer _renderer;

    public ConsoleInputHandler(IGameEngine gameEngine, IBoardRenderer renderer)
    {
        _gameEngine = gameEngine ?? throw new ArgumentNullException(nameof(gameEngine));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    /// <summary>
    /// Processes a line of user input and returns whether the game should continue.
    /// </summary>
    /// <param name="input">Raw user input.</param>
    /// <returns>True to continue game loop, false to exit.</returns>
    public bool HandleInput(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return true;

        var tokens = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
            return true;

        var command = tokens[0].ToLowerInvariant();
        var args = tokens.Skip(1).ToArray();

        try
        {
            return command switch
            {
                "state" => HandleShowState(),
                "units" => HandleShowUnits(),
                "moves" => HandleShowMoves(args),
                "move" => HandleMove(args),
                "attack" => HandleAttack(args),
                "end" => HandleEndTurn(),
                "help" => HandleHelp(),
                "quit" or "exit" => false,
                _ => HandleUnknown(command)
            };
        }
        catch (Exception ex)
        {
            _renderer.RenderError($"Unexpected error: {ex.Message}");
            return true;
        }
    }

    private bool HandleShowState()
    {
        var result = _gameEngine.GetGameState(new GetGameStateQuery());

        if (result.IsFailure)
        {
            _renderer.RenderError(result.ErrorMessage!);
            return true;
        }

        _renderer.RenderGame(result.Value!);
        return true;
    }

    private bool HandleShowUnits()
    {
        var stateResult = _gameEngine.GetGameState(new GetGameStateQuery());
        if (stateResult.IsFailure)
        {
            _renderer.RenderError(stateResult.ErrorMessage!);
            return true;
        }

        var currentPlayerId = stateResult.Value!.CurrentPlayerId;
        var unitsResult = _gameEngine.GetPlayerUnits(new GetPlayerUnitsQuery { PlayerId = currentPlayerId });

        if (unitsResult.IsFailure)
        {
            _renderer.RenderError(unitsResult.ErrorMessage!);
            return true;
        }

        System.Console.WriteLine("Your Units:");
        System.Console.WriteLine("═══════════════════════════════════════════════════════");

        foreach (var unit in unitsResult.Value!)
        {
            System.Console.WriteLine($"  ID: {unit.Id}");
            System.Console.WriteLine($"  Name: {unit.Name}");
            System.Console.WriteLine($"  Position: ({unit.X}, {unit.Y})");
            System.Console.WriteLine($"  Health: {unit.CurrentHealth}/{unit.MaxHealth}");
            System.Console.WriteLine($"  Can Move: {(!unit.HasMovedThisTurn ? "Yes" : "No")}");
            System.Console.WriteLine($"  Can Act: {(!unit.HasActedThisTurn ? "Yes" : "No")}");
            System.Console.WriteLine();
        }

        return true;
    }

    private bool HandleShowMoves(string[] args)
    {
        if (args.Length != 1)
        {
            _renderer.RenderError("Usage: moves <unit-id>");
            return true;
        }

        if (!Guid.TryParse(args[0], out var unitId))
        {
            _renderer.RenderError("Invalid unit ID");
            return true;
        }

        var movesResult = _gameEngine.GetValidMoves(new GetValidMovesQuery { UnitId = unitId });

        if (movesResult.IsFailure)
        {
            _renderer.RenderError(movesResult.ErrorMessage!);
            return true;
        }

        var stateResult = _gameEngine.GetGameState(new GetGameStateQuery());
        if (stateResult.IsFailure)
        {
            _renderer.RenderError(stateResult.ErrorMessage!);
            return true;
        }

        _renderer.RenderGameWithHighlights(stateResult.Value!, movesResult.Value!);

        System.Console.WriteLine($"Valid moves for unit {unitId}:");
        foreach (var pos in movesResult.Value!.ValidPositions)
        {
            System.Console.WriteLine($"  ({pos.X}, {pos.Y})");
        }
        System.Console.WriteLine();

        return true;
    }

    private bool HandleMove(string[] args)
    {
        if (args.Length != 3)
        {
            _renderer.RenderError("Usage: move <unit-id> <x> <y>");
            return true;
        }

        if (!Guid.TryParse(args[0], out var unitId))
        {
            _renderer.RenderError("Invalid unit ID");
            return true;
        }

        if (!int.TryParse(args[1], out var targetX) || !int.TryParse(args[2], out var targetY))
        {
            _renderer.RenderError("Invalid coordinates");
            return true;
        }

        var moveCommand = new MoveUnitCommand
        {
            UnitId = unitId,
            TargetX = targetX,
            TargetY = targetY
        };

        var result = _gameEngine.MoveUnit(moveCommand);

        if (result.IsFailure)
        {
            _renderer.RenderError(result.ErrorMessage!);
            return true;
        }

        _renderer.RenderSuccess($"Unit moved to ({targetX}, {targetY})");
        RefreshDisplay();

        return true;
    }

    private bool HandleAttack(string[] args)
    {
        if (args.Length != 2)
        {
            _renderer.RenderError("Usage: attack <attacker-id> <defender-id>");
            return true;
        }

        if (!Guid.TryParse(args[0], out var attackerId))
        {
            _renderer.RenderError("Invalid attacker ID");
            return true;
        }

        if (!Guid.TryParse(args[1], out var defenderId))
        {
            _renderer.RenderError("Invalid defender ID");
            return true;
        }

        var attackCommand = new AttackCommand
        {
            AttackerId = attackerId,
            DefenderId = defenderId
        };

        var result = _gameEngine.Attack(attackCommand);

        if (result.IsFailure)
        {
            _renderer.RenderError(result.ErrorMessage!);
            return true;
        }

        var attackerResult = _gameEngine.GetUnit(new GetUnitQuery { UnitId = attackerId });
        var defenderResult = _gameEngine.GetUnit(new GetUnitQuery { UnitId = defenderId });

        _renderer.RenderCombatResult(
            result.Value!,
            attackerResult.IsSuccess ? attackerResult.Value : null,
            defenderResult.IsSuccess ? defenderResult.Value : null);

        RefreshDisplay();

        return true;
    }

    private bool HandleEndTurn()
    {
        var stateResult = _gameEngine.GetGameState(new GetGameStateQuery());
        if (stateResult.IsFailure)
        {
            _renderer.RenderError(stateResult.ErrorMessage!);
            return true;
        }

        var endTurnCommand = new EndTurnCommand { PlayerId = stateResult.Value!.CurrentPlayerId };
        var result = _gameEngine.EndTurn(endTurnCommand);

        if (result.IsFailure)
        {
            _renderer.RenderError(result.ErrorMessage!);
            return true;
        }

        _renderer.RenderSuccess("Turn ended");
        RefreshDisplay();

        return true;
    }

    private bool HandleHelp()
    {
        _renderer.RenderHelp();
        return true;
    }

    private bool HandleUnknown(string command)
    {
        _renderer.RenderError($"Unknown command: {command}");
        System.Console.WriteLine("Type 'help' for a list of commands.");
        return true;
    }

    private void RefreshDisplay()
    {
        var stateResult = _gameEngine.GetGameState(new GetGameStateQuery());
        if (stateResult.IsSuccess)
        {
            _renderer.RenderGame(stateResult.Value!);
        }
    }
}