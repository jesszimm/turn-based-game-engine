namespace TurnBasedGame.Application.Commands;

/// <summary>
/// Command to create a new game.
/// </summary>
public sealed record CreateGameCommand
{
    public int BoardWidth { get; init; }
    public int BoardHeight { get; init; }
    public IReadOnlyList<string> PlayerNames { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Command to place a unit on the board during setup.
/// </summary>
public sealed record PlaceUnitCommand
{
    public string UnitName { get; init; } = string.Empty;
    public Guid OwnerId { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
    public int MaxHealth { get; init; }
    public int AttackPower { get; init; }
    public int Defense { get; init; }
    public int MovementRange { get; init; }
}

/// <summary>
/// Command to move a unit to a new position.
/// </summary>
public sealed record MoveUnitCommand
{
    public Guid UnitId { get; init; }
    public int TargetX { get; init; }
    public int TargetY { get; init; }
}

/// <summary>
/// Command to attack another unit.
/// </summary>
public sealed record AttackCommand
{
    public Guid AttackerId { get; init; }
    public Guid DefenderId { get; init; }
}

/// <summary>
/// Command to end the current player's turn.
/// </summary>
public sealed record EndTurnCommand
{
    public Guid PlayerId { get; init; }
}

/// <summary>
/// Command to set terrain type for a tile during setup.
/// </summary>
public sealed record SetTerrainCommand
{
    public int X { get; init; }
    public int Y { get; init; }
    public string TerrainType { get; init; } = string.Empty;
}