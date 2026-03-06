using TurnBasedGame.Domain.Exceptions;
using TurnBasedGame.Domain.ValueObjects;

namespace TurnBasedGame.Domain.Entities;

/// <summary>
/// Represents the game board - a grid of tiles that can contain units.
/// Aggregate root responsible for maintaining board invariants.
/// </summary>
public sealed class GameBoard
{
    private readonly Dictionary<Position, Tile> _tiles;
    private readonly Dictionary<Guid, Unit> _units;

    /// <summary>
    /// Width of the game board in tiles.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Height of the game board in tiles.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// All tiles on the board.
    /// </summary>
    public IReadOnlyCollection<Tile> Tiles => _tiles.Values;

    /// <summary>
    /// Creates a new game board with the specified dimensions.
    /// All tiles are initialized as Plains terrain.
    /// </summary>
    /// <param name="width">Width in tiles (must be positive).</param>
    /// <param name="height">Height in tiles (must be positive).</param>
    public GameBoard(int width, int height)
    {
        if (width <= 0)
            throw new ArgumentException("Width must be positive", nameof(width));
        if (height <= 0)
            throw new ArgumentException("Height must be positive", nameof(height));

        Width = width;
        Height = height;
        _tiles = new Dictionary<Position, Tile>();
        _units = new Dictionary<Guid, Unit>();

        InitializeBoard();
    }

    private void InitializeBoard()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var position = new Position(x, y);
                var tile = new Tile(position, TerrainType.Plains);
                _tiles[position] = tile;
            }
        }
    }

    /// <summary>
    /// Checks if a position is within the board boundaries.
    /// </summary>
    /// <param name="position">Position to check.</param>
    /// <returns>True if the position is valid; otherwise, false.</returns>
    public bool IsValidPosition(Position position)
    {
        if (position == null)
            return false;

        return position.X >= 0 && position.X < Width &&
               position.Y >= 0 && position.Y < Height;
    }

    /// <summary>
    /// Gets the tile at the specified position.
    /// </summary>
    /// <param name="position">Position of the tile.</param>
    /// <returns>The tile at the specified position.</returns>
    /// <exception cref="ArgumentException">Thrown if position is outside board bounds.</exception>
    public Tile GetTile(Position position)
    {
        if (!IsValidPosition(position))
            throw new ArgumentException($"Position {position} is outside board bounds");

        return _tiles[position];
    }

    /// <summary>
    /// Sets the terrain type for a tile at the specified position.
    /// Used during board setup.
    /// </summary>
    /// <param name="position">Position of the tile to modify.</param>
    /// <param name="terrain">New terrain type.</param>
    public void SetTerrain(Position position, TerrainType terrain)
    {
        var currentTile = GetTile(position);
        var occupyingUnitId = currentTile.OccupyingUnitId;

        var newTile = new Tile(position, terrain);

        // Preserve unit occupation
        if (occupyingUnitId.HasValue)
            newTile.PlaceUnit(occupyingUnitId.Value);

        _tiles[position] = newTile;
    }

    /// <summary>
    /// Places a unit on the board at the specified position.
    /// </summary>
    /// <param name="unit">Unit to place.</param>
    /// <param name="position">Position where the unit should be placed.</param>
    /// <exception cref="InvalidMoveException">Thrown if the position is invalid or occupied.</exception>
    public void PlaceUnit(Unit unit, Position position)
    {
        if (unit == null)
            throw new ArgumentNullException(nameof(unit));
        if (position == null)
            throw new ArgumentNullException(nameof(position));

        var tile = GetTile(position);

        if (!tile.IsPassable)
            throw new InvalidMoveException($"Cannot place unit on impassable terrain at {position}");

        if (tile.IsOccupied)
            throw new InvalidMoveException($"Position {position} is already occupied");

        tile.PlaceUnit(unit.Id);
        _units[unit.Id] = unit;
        unit.MoveTo(position);
    }

    /// <summary>
    /// Moves a unit from its current position to a new position.
    /// Validates movement rules and updates both unit and tile state.
    /// </summary>
    /// <param name="unit">Unit to move.</param>
    /// <param name="targetPosition">Destination position.</param>
    /// <exception cref="InvalidMoveException">Thrown if the move violates game rules.</exception>
    public void MoveUnit(Unit unit, Position targetPosition)
    {
        if (unit == null)
            throw new ArgumentNullException(nameof(unit));
        if (targetPosition == null)
            throw new ArgumentNullException(nameof(targetPosition));

        if (!unit.CanMove)
            throw new InvalidMoveException($"Unit {unit.Name} cannot move this turn");

        if (!unit.CanReachPosition(targetPosition))
            throw new InvalidMoveException($"Target position {targetPosition} is out of movement range");

        var targetTile = GetTile(targetPosition);

        if (!targetTile.IsPassable)
            throw new InvalidMoveException($"Target position {targetPosition} is not passable");

        if (targetTile.IsOccupied)
            throw new InvalidMoveException($"Target position {targetPosition} is occupied");

        // Remove unit from current position
        var currentTile = GetTile(unit.Position);
        currentTile.RemoveUnit();

        // Place unit at new position
        targetTile.PlaceUnit(unit.Id);
        unit.MoveTo(targetPosition);
    }

    /// <summary>
    /// Removes a unit from the board (typically when defeated).
    /// </summary>
    /// <param name="unit">Unit to remove.</param>
    public void RemoveUnit(Unit unit)
    {
        if (unit == null)
            throw new ArgumentNullException(nameof(unit));

        var tile = GetTile(unit.Position);
        tile.RemoveUnit();
        _units.Remove(unit.Id);
    }

    /// <summary>
    /// Gets all units currently on the board.
    /// </summary>
    /// <returns>Collection of all units.</returns>
    public IEnumerable<Unit> GetAllUnits()
    {
        return _units.Values;
    }

    /// <summary>
    /// Gets all units belonging to a specific player.
    /// </summary>
    /// <param name="playerId">ID of the player.</param>
    /// <returns>Collection of the player's units.</returns>
    public IEnumerable<Unit> GetPlayerUnits(Guid playerId)
    {
        return _units.Values.Where(u => u.OwnerId == playerId);
    }

    /// <summary>
    /// Finds a unit by its unique identifier.
    /// </summary>
    /// <param name="unitId">ID of the unit to find.</param>
    /// <returns>The unit if found; otherwise, null.</returns>
    public Unit? FindUnit(Guid unitId)
    {
        return _units.TryGetValue(unitId, out var unit) ? unit : null;
    }

    /// <summary>
    /// Gets all valid positions a unit can move to from its current position.
    /// Takes into account movement range, terrain passability, and tile occupation.
    /// </summary>
    /// <param name="unit">Unit to check movement options for.</param>
    /// <returns>Collection of valid destination positions.</returns>
    public IEnumerable<Position> GetValidMovePositions(Unit unit)
    {
        if (unit == null)
            throw new ArgumentNullException(nameof(unit));

        if (!unit.CanMove)
            return Enumerable.Empty<Position>();

        var validPositions = new List<Position>();
        var movementRange = unit.Stats.MovementRange;

        // Simple range check - in a more advanced game, use pathfinding
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var position = new Position(x, y);
                var distance = unit.Position.DistanceTo(position);

                if (distance > 0 && distance <= movementRange)
                {
                    var tile = GetTile(position);
                    if (tile.IsPassable && !tile.IsOccupied)
                    {
                        validPositions.Add(position);
                    }
                }
            }
        }

        return validPositions;
    }
}