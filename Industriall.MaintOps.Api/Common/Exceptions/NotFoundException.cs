namespace Industriall.MaintOps.Api.Common.Exceptions;

/// <summary>
/// Thrown when a requested resource does not exist.
/// Mapped to HTTP 404 by the GlobalExceptionHandler.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string entityName, object id)
        : base($"'{entityName}' with identifier '{id}' was not found.") { }

    public NotFoundException(string message)
        : base(message) { }
}
