namespace TurnBasedGame.Domain.Exceptions;

/// <summary>
/// Thrown when a combat action violates game rules.
/// </summary>
public sealed class InvalidCombatException : DomainException
{
    public InvalidCombatException(string message) : base(message)
    {
    }
}