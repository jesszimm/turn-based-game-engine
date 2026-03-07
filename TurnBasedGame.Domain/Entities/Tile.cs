using TurnBasedGame.Domain.ValueObjects;

namespace TurnBasedGame.Domain.Entities;

/// <summary>
/// Represents a single tile on the game board.
/// Tracks unit occupation.
/// </summary>
public sealed class Tile
{
    /// <summary>
    /// Position of this tile on the game board.
    /// </summary>
    public Position Position { get; }

    /// <summary>
    /// ID of the unit occupying this tile, if any.
    /// </summary>
    public Guid? OccupyingUnitId { get; private set; }

    /// <summary>
    /// Creates a new tile with the specified position.
    /// </summary>
    /// <param name="position">The tile's position on the board.</param>
    public Tile(Position position)
    {
        Position = position ?? throw new ArgumentNullException(nameof(position));
    }

    /// <summary>
    /// Indicates whether a unit is currently on this tile.
    /// </summary>
    public bool IsOccupied => OccupyingUnitId.HasValue;

    /// <summary>
    /// Places a unit on this tile.
    /// </summary>
    /// <param name="unitId">ID of the unit to place.</param>
    /// <exception cref="InvalidOperationException">Thrown if tile is already occupied.</exception>
    internal void PlaceUnit(Guid unitId)
    {
        if (unitId == Guid.Empty)
            throw new ArgumentException("Unit ID cannot be empty", nameof(unitId));

        if (IsOccupied)
            throw new InvalidOperationException($"Tile at {Position} is already occupied");

        OccupyingUnitId = unitId;
    }

    /// <summary>
    /// Removes the unit from this tile.
    /// </summary>
    internal void RemoveUnit()
    {
        OccupyingUnitId = null;
    }

    /// <summary>
    /// Returns a string representation of the tile.
    /// </summary>
    public override string ToString() => Position.ToString();
}
