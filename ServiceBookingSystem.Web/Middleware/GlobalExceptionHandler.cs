using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ServiceBookingSystem.Core.Exceptions;

namespace ServiceBookingSystem.Web.Middleware;

/// <summary>
/// A centralized exception handler that intercepts errors during the HTTP request pipeline.
/// It implements the IExceptionHandler interface introduced in .NET 8.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// Attempts to handle the exception.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="exception">The exception that was thrown.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the exception was handled (response written), False if it should propagate.</returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {

        var requestPath = httpContext.Request.Path;
        if (!requestPath.StartsWithSegments("/api"))
        {
            return false;
        }

        this.logger
            .LogError(exception, "An unhandled exception occurred while processing API request: {Path}",
                requestPath);

        var (statusCode, title) = exception switch
        {
            EntityNotFoundException => (StatusCodes.Status404NotFound, "Resource Not Found"),
            AuthorizationException => (StatusCodes.Status403Forbidden, "Access Denied"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid Request"),
            SlotUnavailableException => (StatusCodes.Status409Conflict, "Slot Unavailable"),
            InvalidBookingStateException => (StatusCodes.Status409Conflict, "Invalid Action"),
            DuplicateEntityException => (StatusCodes.Status409Conflict, "Duplicate Resource"),
            BookingTimeException => (StatusCodes.Status422UnprocessableEntity, "Invalid Timing"),
            ServiceNotActiveException => (StatusCodes.Status422UnprocessableEntity, "Service Unavailable"),
            
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions.Add("traceId", httpContext.TraceIdentifier);

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}