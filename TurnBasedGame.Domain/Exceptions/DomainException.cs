namespace TurnBasedGame.Domain.Exceptions;

/// <summary>
/// Base exception for all domain-specific errors.
/// Indicates a violation of business rules.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }

    protected DomainException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}