namespace TurnBasedGame.Domain.ValueObjects;

/// <summary>
/// Represents the combat and movement statistics of a unit.
/// Immutable value object that defines unit capabilities.
/// </summary>
public sealed record UnitStats
{
    public int MaxHealth { get; init; }
    public int CurrentHealth { get; init; }
    public int AttackPower { get; init; }
    public int Defense { get; init; }
    public int MovementRange { get; init; }

    public UnitStats(
        int maxHealth,
        int attackPower,
        int defense,
        int movementRange,
        int? currentHealth = null)
    {
        if (maxHealth <= 0)
            throw new ArgumentException("Max health must be positive", nameof(maxHealth));
        if (attackPower < 0)
            throw new ArgumentException("Attack power cannot be negative", nameof(attackPower));
        if (defense < 0)
            throw new ArgumentException("Defense cannot be negative", nameof(defense));
        if (movementRange < 0)
            throw new ArgumentException("Movement range cannot be negative", nameof(movementRange));

        MaxHealth = maxHealth;
        CurrentHealth = currentHealth ?? maxHealth;
        AttackPower = attackPower;
        Defense = defense;
        MovementRange = movementRange;

        if (CurrentHealth > MaxHealth)
            throw new ArgumentException("Current health cannot exceed max health");
        if (CurrentHealth < 0)
            throw new ArgumentException("Current health cannot be negative");
    }

    /// <summary>
    /// Returns a new UnitStats with reduced health after taking damage.
    /// </summary>
    public UnitStats TakeDamage(int damage)
    {
        var newHealth = Math.Max(0, CurrentHealth - damage);
        return this with { CurrentHealth = newHealth };
    }

    /// <summary>
    /// Returns a new UnitStats with restored health.
    /// Health cannot exceed MaxHealth.
    /// </summary>
    public UnitStats Heal(int amount)
    {
        var newHealth = Math.Min(MaxHealth, CurrentHealth + amount);
        return this with { CurrentHealth = newHealth };
    }

    public bool IsAlive => CurrentHealth > 0;
    public bool IsFullHealth => CurrentHealth == MaxHealth;
    public double HealthPercentage => (double)CurrentHealth / MaxHealth;
}