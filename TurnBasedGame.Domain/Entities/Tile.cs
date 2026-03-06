using TurnBasedGame.Domain.ValueObjects;

namespace TurnBasedGame.Domain.Entities;

/// <summary>
/// Represents a single tile on the game board.
/// Contains terrain information and tracks unit occupation.
/// </summary>
public sealed class Tile
{
    /// <summary>
    /// Position of this tile on the game board.
    /// </summary>
    public Position Position { get; }

    /// <summary>
    /// Type of terrain on this tile.
    /// </summary>
    public TerrainType Terrain { get; }

    /// <summary>
    /// ID of the unit occupying this tile, if any.
    /// </summary>
    public Guid? OccupyingUnitId { get; private set; }

    /// <summary>
    /// Creates a new tile with the specified position and terrain.
    /// </summary>
    /// <param name="position">The tile's position on the board.</param>
    /// <param name="terrain">The type of terrain.</param>
    public Tile(Position position, TerrainType terrain)
    {
        Position = position ?? throw new ArgumentNullException(nameof(position));
        Terrain = terrain;
    }

    /// <summary>
    /// Indicates whether a unit is currently on this tile.
    /// </summary>
    public bool IsOccupied => OccupyingUnitId.HasValue;

    /// <summary>
    /// Indicates whether units can move through or onto this tile.
    /// </summary>
    public bool IsPassable => Terrain switch
    {
        TerrainType.Plains => true,
        TerrainType.Forest => true,
        TerrainType.Mountain => true,
        TerrainType.Water => false,
        _ => true
    };

    /// <summary>
    /// Gets the movement cost for entering this tile.
    /// Higher values mean it takes more movement points to enter.
    /// </summary>
    /// <returns>Movement cost in movement points.</returns>
    public int GetMovementCost() => Terrain switch
    {
        TerrainType.Plains => 1,
        TerrainType.Forest => 2,
        TerrainType.Mountain => 2,
        TerrainType.Water => int.MaxValue,
        _ => 1
    };

    /// <summary>
    /// Gets the defensive bonus provided by this terrain.
    /// Higher values reduce incoming damage for units on this tile.
    /// </summary>
    /// <returns>Defense bonus value.</returns>
    public int GetDefenseBonus() => Terrain switch
    {
        TerrainType.Plains => 0,
        TerrainType.Forest => 1,
        TerrainType.Mountain => 2,
        TerrainType.Water => 0,
        _ => 0
    };

    /// <summary>
    /// Places a unit on this tile.
    /// </summary>
    /// <param name="unitId">ID of the unit to place.</param>
    /// <exception cref="InvalidOperationException">Thrown if tile is already occupied or not passable.</exception>
    internal void PlaceUnit(Guid unitId)
    {
        if (unitId == Guid.Empty)
            throw new ArgumentException("Unit ID cannot be empty", nameof(unitId));

        if (IsOccupied)
            throw new InvalidOperationException($"Tile at {Position} is already occupied");

        if (!IsPassable)
            throw new InvalidOperationException($"Tile at {Position} is not passable");

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
    public override string ToString() => $"{Position} [{Terrain}]";
}