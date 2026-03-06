namespace TurnBasedGame.Application.DTOs;

/// <summary>
/// Data transfer object representing the complete game state.
/// Immutable snapshot for queries and display purposes.
/// </summary>
public sealed record GameStateDto
{
    public int BoardWidth { get; init; }
    public int BoardHeight { get; init; }
    public IReadOnlyList<TileDto> Tiles { get; init; } = Array.Empty<TileDto>();
    public IReadOnlyList<UnitDto> Units { get; init; } = Array.Empty<UnitDto>();
    public IReadOnlyList<PlayerDto> Players { get; init; } = Array.Empty<PlayerDto>();
    public Guid CurrentPlayerId { get; init; }
    public int TurnNumber { get; init; }
}

/// <summary>
/// Data transfer object for a single tile.
/// </summary>
public sealed record TileDto
{
    public int X { get; init; }
    public int Y { get; init; }
    public string Terrain { get; init; } = string.Empty;
    public bool IsOccupied { get; init; }
    public Guid? OccupyingUnitId { get; init; }
    public int MovementCost { get; init; }
    public int DefenseBonus { get; init; }
}

/// <summary>
/// Data transfer object for a unit.
/// </summary>
public sealed record UnitDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid OwnerId { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
    public int CurrentHealth { get; init; }
    public int MaxHealth { get; init; }
    public int AttackPower { get; init; }
    public int Defense { get; init; }
    public int MovementRange { get; init; }
    public bool HasMovedThisTurn { get; init; }
    public bool HasActedThisTurn { get; init; }
    public bool IsAlive { get; init; }
}

/// <summary>
/// Data transfer object for a player.
/// </summary>
public sealed record PlayerDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int UnitCount { get; init; }
}

/// <summary>
/// Data transfer object for valid move positions.
/// </summary>
public sealed record ValidMovesDto
{
    public Guid UnitId { get; init; }
    public IReadOnlyList<PositionDto> ValidPositions { get; init; } = Array.Empty<PositionDto>();
}

/// <summary>
/// Data transfer object for a position.
/// </summary>
public sealed record PositionDto
{
    public int X { get; init; }
    public int Y { get; init; }
}

/// <summary>
/// Data transfer object for combat results.
/// </summary>
public sealed record CombatResultDto
{
    public Guid AttackerId { get; init; }
    public Guid DefenderId { get; init; }
    public int DamageDealt { get; init; }
    public int DefenderHealthRemaining { get; init; }
    public bool DefenderDefeated { get; init; }
}