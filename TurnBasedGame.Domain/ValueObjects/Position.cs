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
    /// Checks whether another position is adjacent (optionally including diagonals).
    /// </summary>
    public bool IsAdjacentTo(Position other, bool includeDiagonals = false)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        var deltaX = Math.Abs(X - other.X);
        var deltaY = Math.Abs(Y - other.Y);

        if (deltaX == 0 && deltaY == 0)
            return false;

        if (includeDiagonals)
            return deltaX <= 1 && deltaY <= 1;

        return deltaX + deltaY == 1;
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
