namespace Industriall.MaintOps.Api.Common.Exceptions;

/// <summary>
/// Thrown when a domain rule (business invariant) is violated.
/// Mapped to HTTP 422 Unprocessable Entity by the GlobalExceptionHandler.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message)
        : base(message) { }
}
