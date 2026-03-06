using TurnBasedGame.Domain.Entities;

namespace TurnBasedGame.Domain.Interfaces;

/// <summary>
/// Defines combat resolution logic.
/// Abstraction allows for different combat systems (simple, complex, randomized).
/// </summary>
public interface ICombatResolver
{
    /// <summary>
    /// Calculates the damage dealt by an attacker to a defender.
    /// Takes into account unit stats and terrain modifiers.
    /// </summary>
    /// <param name="attacker">The attacking unit</param>
    /// <param name="defender">The defending unit</param>
    /// <param name="defenderTile">The tile the defender is standing on</param>
    /// <returns>The amount of damage to apply to the defender</returns>
    int CalculateDamage(Unit attacker, Unit defender, Tile defenderTile);

    /// <summary>
    /// Executes a combat action between two units.
    /// Updates unit states based on combat resolution.
    /// </summary>
    /// <param name="attacker">The attacking unit</param>
    /// <param name="defender">The defending unit</param>
    /// <param name="defenderTile">The tile the defender is standing on</param>
    void ResolveCombat(Unit attacker, Unit defender, Tile defenderTile);
}