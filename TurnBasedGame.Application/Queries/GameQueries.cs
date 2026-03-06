namespace TurnBasedGame.Application.Queries;

/// <summary>
/// Query to get the complete game state.
/// </summary>
public sealed record GetGameStateQuery
{
    // No parameters - returns full state
}

/// <summary>
/// Query to get valid move positions for a specific unit.
/// </summary>
public sealed record GetValidMovesQuery
{
    public Guid UnitId { get; init; }
}

/// <summary>
/// Query to get all units belonging to a specific player.
/// </summary>
public sealed record GetPlayerUnitsQuery
{
    public Guid PlayerId { get; init; }
}

/// <summary>
/// Query to check if a player can perform any actions.
/// </summary>
public sealed record CanPlayerActQuery
{
    public Guid PlayerId { get; init; }
}

/// <summary>
/// Query to get information about a specific unit.
/// </summary>
public sealed record GetUnitQuery
{
    public Guid UnitId { get; init; }
}