using TurnBasedGame.Domain.Exceptions;
using TurnBasedGame.Domain.ValueObjects;
using TurnBasedGame.Domain.Interfaces;

namespace TurnBasedGame.Domain.Entities;

/// <summary>
/// Represents the game board - a grid of tiles that can contain units.
/// Aggregate root responsible for maintaining board invariants.
/// Default size is 5x5 but can be configured.
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
    /// Creates a new 5x5 game board.
    /// All tiles are initialized as Plains terrain.
    /// </summary>
    public GameBoard() : this(5, 5)
    {
    }

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
    /// Prevents movement outside the board.
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
            throw new ArgumentException($"Position {position} is outside board bounds (0-{Width-1}, 0-{Height-1})");

        return _tiles[position];
    }

    /// <summary>
    /// Gets the unit at the specified position, if any.
    /// </summary>
    /// <param name="position">Position to check.</param>
    /// <returns>The unit at the position, or null if no unit is there.</returns>
    public Unit? GetUnitAtPosition(Position position)
    {
        if (!IsValidPosition(position))
            return null;

        var tile = _tiles[position];
        if (!tile.IsOccupied)
            return null;

        return _units.TryGetValue(tile.OccupyingUnitId!.Value, out var unit) ? unit : null;
    }

    /// <summary>
    /// Checks if a position is occupied by a unit.
    /// Prevents movement onto occupied tiles.
    /// </summary>
    /// <param name="position">Position to check.</param>
    /// <returns>True if a unit occupies the position; otherwise, false.</returns>
    public bool IsPositionOccupied(Position position)
    {
        if (!IsValidPosition(position))
            return false;

        return _tiles[position].IsOccupied;
    }

    /// <summary>
    /// Places a unit on the board at the specified position.
    /// Validates that the position is valid, passable, and unoccupied.
    /// </summary>
    /// <param name="unit">Unit to place.</param>
    /// <param name="position">Position where the unit should be placed.</param>
    /// <exception cref="InvalidMoveException">Thrown if the position is invalid, impassable, or occupied.</exception>
    public void PlaceUnit(Unit unit, Position position)
    {
        if (unit == null)
            throw new ArgumentNullException(nameof(unit));
        if (position == null)
            throw new ArgumentNullException(nameof(position));

        // Prevent placement outside the board
        if (!IsValidPosition(position))
            throw new InvalidMoveException($"Cannot place unit outside board bounds at {position}");

        var tile = _tiles[position];

        // Prevent placement on impassable terrain
        if (!tile.IsPassable)
            throw new InvalidMoveException($"Cannot place unit on impassable terrain at {position}");

        // Prevent placement on occupied tile
        if (tile.IsOccupied)
            throw new InvalidMoveException($"Position {position} is already occupied");

        // Place the unit
        tile.PlaceUnit(unit.Id);
        _units[unit.Id] = unit;
    }

    /// <summary>
    /// Moves a unit to an adjacent tile.
    /// Validates movement rules: must be adjacent, within bounds, passable, and unoccupied.
    /// </summary>
    /// <param name="unit">Unit to move.</param>
    /// <param name="targetPosition">Destination position (must be adjacent).</param>
    /// <exception cref="InvalidMoveException">Thrown if the move violates game rules.</exception>
    public void MoveUnitToAdjacentTile(Unit unit, Position targetPosition)
    {
        if (unit == null)
            throw new ArgumentNullException(nameof(unit));
        if (targetPosition == null)
            throw new ArgumentNullException(nameof(targetPosition));

        // Check if unit can move
        if (!unit.CanMove)
            throw new InvalidMoveException($"Unit {unit.Name} has already moved this turn");

        // Prevent movement outside the board
        if (!IsValidPosition(targetPosition))
            throw new InvalidMoveException($"Target position {targetPosition} is outside board bounds (0-{Width-1}, 0-{Height-1})");

        // Check if target is adjacent (distance of 1)
        var distance = unit.Position.DistanceTo(targetPosition);
        if (distance != 1)
            throw new InvalidMoveException($"Target position {targetPosition} is not adjacent to current position {unit.Position}");

        var targetTile = _tiles[targetPosition];

        // Prevent movement onto impassable terrain
        if (!targetTile.IsPassable)
            throw new InvalidMoveException($"Target position {targetPosition} is not passable ({targetTile.Terrain})");

        // Prevent movement onto occupied tile
        if (targetTile.IsOccupied)
            throw new InvalidMoveException($"Target position {targetPosition} is already occupied");

        // Execute the move
        var currentTile = _tiles[unit.Position];
        currentTile.RemoveUnit();
        targetTile.PlaceUnit(unit.Id);
        unit.MoveTo(targetPosition);
    }

    /// <summary>
    /// Moves a unit to a new position (with range checking).
    /// Uses the unit's movement range to determine if the move is valid.
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
            throw new InvalidMoveException($"Unit {unit.Name} has already moved this turn");

        // Prevent movement outside the board
        if (!IsValidPosition(targetPosition))
            throw new InvalidMoveException($"Target position {targetPosition} is outside board bounds (0-{Width-1}, 0-{Height-1})");

        if (!unit.CanReachPosition(targetPosition))
            throw new InvalidMoveException($"Target position {targetPosition} is out of movement range");

        var targetTile = _tiles[targetPosition];

        // Prevent movement onto impassable terrain
        if (!targetTile.IsPassable)
            throw new InvalidMoveException($"Target position {targetPosition} is not passable ({targetTile.Terrain})");

        // Prevent movement onto occupied tile
        if (targetTile.IsOccupied)
            throw new InvalidMoveException($"Target position {targetPosition} is already occupied");

        // Execute the move
        var currentTile = _tiles[unit.Position];
        currentTile.RemoveUnit();
        targetTile.PlaceUnit(unit.Id);
        unit.MoveTo(targetPosition);
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

        // Check all positions within movement range
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var position = new Position(x, y);
                var distance = unit.Position.DistanceTo(position);

                if (distance > 0 && distance <= movementRange)
                {
                    var tile = _tiles[position];
                    if (tile.IsPassable && !tile.IsOccupied)
                    {
                        validPositions.Add(position);
                    }
                }
            }
        }

        return validPositions;
    }

    /// <summary>
    /// Gets all adjacent positions to a given position (up, down, left, right).
    /// Only returns positions that are within board bounds.
    /// </summary>
    /// <param name="position">The center position.</param>
    /// <returns>Collection of adjacent positions within bounds.</returns>
    public IEnumerable<Position> GetAdjacentPositions(Position position)
    {
        if (position == null)
            throw new ArgumentNullException(nameof(position));

        return position.GetAdjacentPositions()
            .Where(IsValidPosition);
    }

    /// <summary>
    /// Executes an attack from one unit to another.
    /// Validates attack range using Manhattan distance, calculates damage,
    /// reduces target health, and removes the target if health <= 0.
    /// </summary>
    /// <param name="attacker">The attacking unit.</param>
    /// <param name="defender">The defending unit.</param>
    /// <param name="combatResolver">Combat resolver to calculate damage.</param>
    /// <returns>The amount of damage dealt.</returns>
    /// <exception cref="InvalidCombatException">Thrown if the attack violates combat rules.</exception>
    public int Attack(Unit attacker, Unit defender, ICombatResolver combatResolver)
    {
        if (attacker == null)
            throw new ArgumentNullException(nameof(attacker));
        if (defender == null)
            throw new ArgumentNullException(nameof(defender));
        if (combatResolver == null)
            throw new ArgumentNullException(nameof(combatResolver));

        // Validate attacker can act
        if (!attacker.CanAct)
            throw new InvalidCombatException($"Unit {attacker.Name} has already acted this turn");

        // Validate defender is alive
        if (!defender.IsAlive)
            throw new InvalidCombatException($"Cannot attack defeated unit {defender.Name}");

        // Validate attacker cannot attack own units
        if (attacker.OwnerId == defender.OwnerId)
            throw new InvalidCombatException($"Unit {attacker.Name} cannot attack friendly unit {defender.Name}");

        // Calculate Manhattan distance
        var distance = attacker.Position.DistanceTo(defender.Position);

        // Validate attack range (currently melee only - adjacent tiles)
        if (distance != 1)
            throw new InvalidCombatException(
                $"Target {defender.Name} at {defender.Position} is not within attack range " +
                $"(distance: {distance}, required: 1)");

        // Get defender's tile for terrain bonus calculation
        var defenderTile = GetTile(defender.Position);

        // Calculate damage using combat resolver
        var damage = combatResolver.CalculateDamage(attacker, defender, defenderTile);

        // Apply damage to defender
        defender.TakeDamage(damage);

        // Mark attacker as having acted
        attacker.MarkAsActed();

        // Remove unit from board if health <= 0
        if (!defender.IsAlive)
        {
            RemoveUnit(defender);
        }

        return damage;
    }

    /// <summary>
    /// Checks if a unit can attack another unit.
    /// Validates range, attacker status, and ownership.
    /// </summary>
    /// <param name="attacker">The attacking unit.</param>
    /// <param name="defender">The defending unit.</param>
    /// <returns>True if the attack is valid; otherwise, false.</returns>
    public bool CanAttack(Unit attacker, Unit defender)
    {
        if (attacker == null || defender == null)
            return false;

        if (!attacker.CanAct)
            return false;

        if (!defender.IsAlive)
            return false;

        if (attacker.OwnerId == defender.OwnerId)
            return false;

        // Check if defender is within attack range (melee = adjacent)
        var distance = attacker.Position.DistanceTo(defender.Position);
        return distance == 1;
    }

    /// <summary>
    /// Gets all units that a given unit can attack.
    /// Returns only enemy units within attack range.
    /// </summary>
    /// <param name="attacker">The unit to check attack targets for.</param>
    /// <returns>Collection of units that can be attacked.</returns>
    public IEnumerable<Unit> GetValidAttackTargets(Unit attacker)
    {
        if (attacker == null)
            throw new ArgumentNullException(nameof(attacker));

        if (!attacker.CanAct)
            return Enumerable.Empty<Unit>();

        return GetAllUnits()
            .Where(target => CanAttack(attacker, target));
    }
}
