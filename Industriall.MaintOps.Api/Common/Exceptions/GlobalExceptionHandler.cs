using Industriall.MaintOps.Api.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Industriall.MaintOps.Api.Common.Exceptions;

/// <summary>
/// Centralized exception handler that maps exceptions to RFC 7807 Problem Details responses.
/// Registered via app.UseExceptionHandler() in Program.cs.
/// </summary>
internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext          httpContext,
        Exception            exception,
        CancellationToken    cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var (statusCode, title) = exception switch
        {
            FluentValidation.ValidationException => (StatusCodes.Status400BadRequest,            "Validation Error"),
            NotFoundException                    => (StatusCodes.Status404NotFound,              "Resource Not Found"),
            DomainException                      => (StatusCodes.Status422UnprocessableEntity,   "Domain Rule Violation"),
            UnauthorizedAccessException          => (StatusCodes.Status401Unauthorized,          "Unauthorized"),
            _                                    => (StatusCodes.Status500InternalServerError,   "Internal Server Error")
        };

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status   = statusCode,
            Title    = title,
            Detail   = exception.Message,
            Instance = httpContext.Request.Path
        };

        // Enrich validation responses with per-field error details.
        if (exception is FluentValidation.ValidationException validationException)
        {
            problemDetails.Extensions["errors"] = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(e => e.ErrorMessage).ToArray());
        }

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true; // Signal that the exception has been handled.
    }
}
