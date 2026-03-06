namespace TurnBasedGame.Domain.ValueObjects;

/// <summary>
/// Represents an immutable position on the game board.
/// Value object - equality is based on coordinate values, not identity.
/// </summary>
public sealed record Position
{
    public int X { get; }
    public int Y { get; }

    public Position(int x, int y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Calculates the Manhattan distance to another position.
    /// Used for movement range and attack range calculations.
    /// </summary>
    public int DistanceTo(Position other)
    {
        return Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
    }

    /// <summary>
    /// Gets all positions adjacent to this one (up, down, left, right).
    /// Does not include diagonal positions.
    /// </summary>
    public IEnumerable<Position> GetAdjacentPositions()
    {
        yield return new Position(X, Y - 1); // North
        yield return new Position(X + 1, Y); // East
        yield return new Position(X, Y + 1); // South
        yield return new Position(X - 1, Y); // West
    }

    public override string ToString() => $"({X}, {Y})";
}