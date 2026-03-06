namespace TurnBasedGame.Domain.ValueObjects;

/// <summary>
/// Represents different types of terrain that affect movement and combat.
/// </summary>
public enum TerrainType
{
    /// <summary>
    /// Normal passable terrain with no modifiers.
    /// </summary>
    Plains,

    /// <summary>
    /// Rough terrain that costs extra movement.
    /// </summary>
    Forest,

    /// <summary>
    /// Elevated terrain that provides defense bonus.
    /// </summary>
    Mountain,

    /// <summary>
    /// Impassable terrain that blocks movement.
    /// </summary>
    Water
}