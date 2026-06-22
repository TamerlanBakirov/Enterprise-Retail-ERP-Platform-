using System.Net;
using System.Text.Json;
using FluentValidation;
using GeorgiaERP.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace GeorgiaERP.Api.Middleware;

/// <summary>
/// Global exception handling middleware that converts all unhandled exceptions
/// into RFC 7807 ProblemDetails responses for consistent API error contracts.
/// Each error includes an "errorCode" extension for client-side programmatic handling.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Validation error on {Method} {Path}: {ErrorCount} error(s)",
                context.Request.Method, context.Request.Path, ex.Errors.Count());

            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            var problem = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "One or more validation errors occurred.",
                Status = (int)HttpStatusCode.BadRequest,
                Instance = context.Request.Path
            };
            problem.Extensions["errorCode"] = ErrorCodes.ValidationError;
            problem.Extensions["errors"] = errors;
            problem.Extensions["traceId"] = context.TraceIdentifier;

            await WriteProblemDetailsAsync(context, problem);
        }
        catch (UnauthorizedAccessException)
        {
            // SECURITY: Do not log exception details for auth failures - prevents info leakage
            _logger.LogWarning("Unauthorized access on {Method} {Path} from {IpAddress}",
                context.Request.Method, context.Request.Path,
                context.Connection.RemoteIpAddress);

            var problem = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                Title = "Access denied.",
                Status = (int)HttpStatusCode.Forbidden,
                Detail = "You do not have permission to access this resource.",
                Instance = context.Request.Path
            };
            problem.Extensions["errorCode"] = ErrorCodes.Forbidden;
            problem.Extensions["traceId"] = context.TraceIdentifier;

            await WriteProblemDetailsAsync(context, problem);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Resource not found on {Method} {Path}: {Message}",
                context.Request.Method, context.Request.Path, ex.Message);

            var problem = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                Title = "Resource not found.",
                Status = (int)HttpStatusCode.NotFound,
                Detail = ex.Message,
                Instance = context.Request.Path
            };
            problem.Extensions["errorCode"] = ErrorCodes.NotFound;
            problem.Extensions["traceId"] = context.TraceIdentifier;

            await WriteProblemDetailsAsync(context, problem);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("conflict", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Conflict on {Method} {Path}: {Message}",
                context.Request.Method, context.Request.Path, ex.Message);

            var problem = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                Title = "A conflict occurred.",
                Status = (int)HttpStatusCode.Conflict,
                Detail = ex.Message,
                Instance = context.Request.Path
            };
            problem.Extensions["errorCode"] = ErrorCodes.Conflict;
            problem.Extensions["traceId"] = context.TraceIdentifier;

            await WriteProblemDetailsAsync(context, problem);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("insufficient stock", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Insufficient stock on {Method} {Path}: {Message}",
                context.Request.Method, context.Request.Path, ex.Message);

            var problem = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Insufficient stock.",
                Status = (int)HttpStatusCode.BadRequest,
                Detail = ex.Message,
                Instance = context.Request.Path
            };
            problem.Extensions["errorCode"] = ErrorCodes.InsufficientStock;
            problem.Extensions["traceId"] = context.TraceIdentifier;

            await WriteProblemDetailsAsync(context, problem);
        }
        catch (Exception ex)
        {
            // Generate a correlation ID so the error can be traced in logs without
            // exposing internal details to the client (OWASP A09:2021).
            var correlationId = context.Items["CorrelationId"]?.ToString()
                ?? Guid.NewGuid().ToString("N")[..12];

            _logger.LogError(ex, "Unhandled exception [{CorrelationId}] on {Method} {Path}",
                correlationId, context.Request.Method, context.Request.Path);

            // In development, include exception details for debugging.
            // In production, return only the correlation ID.
            var env = context.RequestServices.GetService<IHostEnvironment>();
            var detail = env?.IsDevelopment() == true
                ? $"{ex.GetType().Name}: {ex.Message}"
                : "An unexpected error occurred. If this persists, contact support.";

            var problem = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "An internal server error occurred.",
                Status = (int)HttpStatusCode.InternalServerError,
                Detail = detail,
                Instance = context.Request.Path
            };
            problem.Extensions["errorCode"] = ErrorCodes.InternalError;
            problem.Extensions["correlationId"] = correlationId;
            problem.Extensions["traceId"] = context.TraceIdentifier;

            await WriteProblemDetailsAsync(context, problem);
        }
    }

    private static async Task WriteProblemDetailsAsync(HttpContext context, ProblemDetails problem)
    {
        context.Response.StatusCode = problem.Status ?? 500;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }
}
