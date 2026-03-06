namespace TurnBasedGame.Domain.Entities;

/// <summary>
/// Represents a player in the game.
/// Entity with identity-based equality.
/// </summary>
public sealed class Player
{
    /// <summary>
    /// Unique identifier for this player.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Display name of the player.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Indicates whether the player is still active in the game.
    /// A player becomes inactive when they have no living units.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Creates a new player with a generated ID.
    /// </summary>
    /// <param name="name">The player's display name.</param>
    public Player(string name) : this(Guid.NewGuid(), name, isActive: true)
    {
    }

    /// <summary>
    /// Creates a new player with a specified ID and active state.
    /// Used for reconstituting players from persistence.
    /// </summary>
    /// <param name="id">Unique identifier for the player.</param>
    /// <param name="name">The player's display name.</param>
    /// <param name="isActive">Whether the player is currently active.</param>
    public Player(Guid id, string name, bool isActive = true)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Player ID cannot be empty", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Player name cannot be empty", nameof(name));

        Id = id;
        Name = name;
        IsActive = isActive;
    }

    /// <summary>
    /// Marks the player as inactive.
    /// Called when the player has no living units remaining.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Marks the player as active.
    /// Could be used for game restart or restoration scenarios.
    /// </summary>
    internal void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Determines equality based on player ID.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is Player other && Id == other.Id;
    }

    /// <summary>
    /// Returns hash code based on player ID.
    /// </summary>
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    /// <summary>
    /// Returns a string representation of the player.
    /// </summary>
    public override string ToString() => Name;
}