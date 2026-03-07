using TurnBasedGame.Domain.ValueObjects;

namespace TurnBasedGame.Domain.Entities;

/// <summary>
/// Represents a combat unit on the game board.
/// Entity with identity-based equality and mutable state.
/// </summary>
public sealed class Unit
{
    /// <summary>
    /// Unique identifier for this unit.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Display name of the unit.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Identifier of the player who owns this unit.
    /// </summary>
    public Guid OwnerId { get; }

    /// <summary>
    /// Current position on the game board.
    /// </summary>
    public Position Position { get; private set; }

    /// <summary>
    /// Combat and movement statistics.
    /// </summary>
    public UnitStats Stats { get; private set; }

    /// <summary>
    /// Indicates whether the unit has moved during the current turn.
    /// </summary>
    public bool HasMovedThisTurn { get; private set; }

    /// <summary>
    /// Indicates whether the unit has performed an action during the current turn.
    /// </summary>
    public bool HasActedThisTurn { get; private set; }

    /// <summary>
    /// Creates a new unit with a generated ID.
    /// </summary>
    public Unit(
        string name,
        Guid ownerId,
        Position position,
        UnitStats stats)
        : this(Guid.NewGuid(), name, ownerId, position, stats)
    {
    }

    /// <summary>
    /// Creates a new unit with a specified ID.
    /// Used for reconstituting units from persistence.
    /// </summary>
    public Unit(
        Guid id,
        string name,
        Guid ownerId,
        Position position,
        UnitStats stats)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Unit ID cannot be empty", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Unit name cannot be empty", nameof(name));
        if (ownerId == Guid.Empty)
            throw new ArgumentException("Owner ID cannot be empty", nameof(ownerId));

        Id = id;
        Name = name;
        OwnerId = ownerId;
        Position = position ?? throw new ArgumentNullException(nameof(position));
        Stats = stats ?? throw new ArgumentNullException(nameof(stats));
    }

    /// <summary>
    /// Indicates whether the unit is alive (has health remaining).
    /// </summary>
    public bool IsAlive => Stats.IsAlive;

    /// <summary>
    /// Indicates whether the unit can perform an action this turn.
    /// </summary>
    public bool CanAct => IsAlive && !HasActedThisTurn;

    /// <summary>
    /// Indicates whether the unit can move this turn.
    /// </summary>
    public bool CanMove => IsAlive && !HasMovedThisTurn;

    /// <summary>
    /// Checks if this unit can reach the specified position based on movement range.
    /// Does not validate tile occupation.
    /// </summary>
    /// <param name="target">The target position to check.</param>
    /// <returns>True if the position is within movement range; otherwise, false.</returns>
    public bool CanReachPosition(Position target)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        if (!CanMove)
            return false;

        var distance = Position.DistanceTo(target);
        return distance > 0 && distance <= Stats.MovementRange;
    }

    /// <summary>
    /// Checks if this unit can attack a target at the specified position.
    /// Uses melee range (adjacent tiles, including diagonals).
    /// </summary>
    /// <param name="targetPosition">Position of the target unit.</param>
    /// <param name="targetOwnerId">Owner ID of the target unit.</param>
    /// <returns>True if the target can be attacked; otherwise, false.</returns>
    public bool CanAttackPosition(Position targetPosition, Guid targetOwnerId)
    {
        if (targetPosition == null)
            throw new ArgumentNullException(nameof(targetPosition));

        if (!CanAct)
            return false;

        if (targetOwnerId == OwnerId)
            return false; // Cannot attack own units

        // Melee range: 8-direction adjacency
        return Position.IsAdjacentTo(targetPosition, includeDiagonals: true);
    }

    /// <summary>
    /// Moves the unit to a new position.
    /// Marks the unit as having moved this turn.
    /// </summary>
    /// <param name="newPosition">The destination position.</param>
    internal void MoveTo(Position newPosition)
    {
        if (newPosition == null)
            throw new ArgumentNullException(nameof(newPosition));

        Position = newPosition;
        HasMovedThisTurn = true;
    }

    /// <summary>
    /// Applies damage to this unit, reducing its health.
    /// Updates the unit's stats with the new health value.
    /// </summary>
    /// <param name="damage">Amount of damage to apply.</param>
    internal void TakeDamage(int damage)
    {
        if (damage < 0)
            throw new ArgumentException("Damage cannot be negative", nameof(damage));

        Stats = Stats.TakeDamage(damage);
    }

    /// <summary>
    /// Heals the unit by the specified amount.
    /// Health cannot exceed maximum health.
    /// </summary>
    /// <param name="amount">Amount of health to restore.</param>
    internal void Heal(int amount)
    {
        if (amount < 0)
            throw new ArgumentException("Heal amount cannot be negative", nameof(amount));

        Stats = Stats.Heal(amount);
    }

    /// <summary>
    /// Marks this unit as having acted during the current turn.
    /// </summary>
    internal void MarkAsActed()
    {
        HasActedThisTurn = true;
    }

    /// <summary>
    /// Resets turn-based state flags at the start of a new turn.
    /// </summary>
    public void ResetTurnState()
    {
        HasMovedThisTurn = false;
        HasActedThisTurn = false;
    }

    /// <summary>
    /// Determines equality based on unit ID.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is Unit other && Id == other.Id;
    }

    /// <summary>
    /// Returns hash code based on unit ID.
    /// </summary>
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    /// <summary>
    /// Returns a string representation of the unit.
    /// </summary>
    public override string ToString() => $"{Name} (HP: {Stats.CurrentHealth}/{Stats.MaxHealth})";
}
