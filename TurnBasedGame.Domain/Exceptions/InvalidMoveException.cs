namespace TurnBasedGame.Domain.Exceptions;

/// <summary>
/// Thrown when a unit attempts an invalid move operation.
/// </summary>
public sealed class InvalidMoveException : DomainException
{
    public InvalidMoveException(string message) : base(message)
    {
    }
}