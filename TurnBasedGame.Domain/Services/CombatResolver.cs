using TurnBasedGame.Domain.Entities;
using TurnBasedGame.Domain.Exceptions;
using TurnBasedGame.Domain.Interfaces;

namespace TurnBasedGame.Domain.Services;

/// <summary>
/// Simple deterministic combat resolver.
/// Damage = Attacker.Attack
/// No randomness - makes testing easier and gameplay more predictable.
/// </summary>
public sealed class CombatResolver : ICombatResolver
{
    private const int MinimumDamage = 1;

    /// <summary>
    /// Calculates the damage dealt by an attacker to a defender.
    /// </summary>
    /// <param name="attacker">The attacking unit.</param>
    /// <param name="defender">The defending unit.</param>
    /// <returns>Amount of damage to apply.</returns>
    public int CalculateDamage(Unit attacker, Unit defender)
    {
        if (attacker == null)
            throw new ArgumentNullException(nameof(attacker));
        if (defender == null)
            throw new ArgumentNullException(nameof(defender));

        var attackPower = attacker.Stats.AttackPower;

        // Always deal at least minimum damage if attack goes through
        return Math.Max(attackPower, MinimumDamage);
    }

    /// <summary>
    /// Executes combat between two units, applying damage and marking the attacker as having acted.
    /// </summary>
    /// <param name="attacker">The attacking unit.</param>
    /// <param name="defender">The defending unit.</param>
    /// <exception cref="InvalidCombatException">Thrown if the attack violates combat rules.</exception>
    public void ResolveCombat(Unit attacker, Unit defender)
    {
        if (attacker == null)
            throw new ArgumentNullException(nameof(attacker));
        if (defender == null)
            throw new ArgumentNullException(nameof(defender));

        if (!attacker.CanAttackPosition(defender.Position, defender.OwnerId))
            throw new InvalidCombatException(
                $"Unit {attacker.Name} cannot attack {defender.Name}");

        var damage = CalculateDamage(attacker, defender);

        defender.TakeDamage(damage);
        attacker.MarkAsActed();
    }
}
