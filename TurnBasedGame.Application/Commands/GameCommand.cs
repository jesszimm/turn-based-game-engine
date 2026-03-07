namespace TurnBasedGame.Application.Commands;

/// <summary>
/// Command to move a unit to a target position.
/// Contains the acting unit ID and target coordinates.
/// </summary>
public sealed record MoveUnitCommand
{
    /// <summary>
    /// ID of the unit to move.
    /// </summary>
    public Guid UnitId { get; init; }

    /// <summary>
    /// Target X coordinate.
    /// </summary>
    public int TargetX { get; init; }

    /// <summary>
    /// Target Y coordinate.
    /// </summary>
    public int TargetY { get; init; }

    /// <summary>
    /// Creates a new move command.
    /// </summary>
    public MoveUnitCommand(Guid unitId, int targetX, int targetY)
    {
        UnitId = unitId;
        TargetX = targetX;
        TargetY = targetY;
    }

    /// <summary>
    /// Parameterless constructor for record initialization.
    /// </summary>
    public MoveUnitCommand() { }
}

/// <summary>
/// Command to attack a target unit.
/// Contains the attacking unit ID and defender unit ID.
/// </summary>
public sealed record AttackUnitCommand
{
    /// <summary>
    /// ID of the attacking unit.
    /// </summary>
    public Guid AttackerId { get; init; }

    /// <summary>
    /// ID of the target unit to attack.
    /// </summary>
    public Guid DefenderId { get; init; }

    /// <summary>
    /// Creates a new attack command.
    /// </summary>
    public AttackUnitCommand(Guid attackerId, Guid defenderId)
    {
        AttackerId = attackerId;
        DefenderId = defenderId;
    }

    /// <summary>
    /// Parameterless constructor for record initialization.
    /// </summary>
    public AttackUnitCommand() { }
}

/// <summary>
/// Command to end the current player's turn.
/// </summary>
public sealed record EndTurnCommand
{
    /// <summary>
    /// Creates a new end turn command.
    /// </summary>
    public EndTurnCommand() { }
}

/// <summary>
/// Command to create a new game.
/// </summary>
public sealed record CreateGameCommand
{
    /// <summary>
    /// Name of player 1.
    /// </summary>
    public string Player1Name { get; init; } = string.Empty;

    /// <summary>
    /// Name of player 2.
    /// </summary>
    public string Player2Name { get; init; } = string.Empty;

    /// <summary>
    /// Optional board width (defaults to 5).
    /// </summary>
    public int BoardWidth { get; init; } = 5;

    /// <summary>
    /// Optional board height (defaults to 5).
    /// </summary>
    public int BoardHeight { get; init; } = 5;
}

/// <summary>
/// Command to place a unit on the board during setup.
/// </summary>
public sealed record PlaceUnitCommand
{
    /// <summary>
    /// Name of the unit.
    /// </summary>
    public string UnitName { get; init; } = string.Empty;

    /// <summary>
    /// ID of the owning player.
    /// </summary>
    public Guid PlayerId { get; init; }

    /// <summary>
    /// X coordinate to place the unit.
    /// </summary>
    public int X { get; init; }

    /// <summary>
    /// Y coordinate to place the unit.
    /// </summary>
    public int Y { get; init; }

    /// <summary>
    /// Maximum health of the unit.
    /// </summary>
    public int MaxHealth { get; init; }

    /// <summary>
    /// Attack power of the unit.
    /// </summary>
    public int AttackPower { get; init; }

    /// <summary>
    /// Defense value of the unit.
    /// </summary>
    public int Defense { get; init; }

    /// <summary>
    /// Movement range of the unit.
    /// </summary>
    public int MovementRange { get; init; }
}
