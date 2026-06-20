using System.Net;
using System.Text.Json;
using FluentValidation;

namespace GeorgiaERP.Api.Middleware;

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

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";

            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                type = "ValidationError",
                errors
            }, JsonOptions));
        }
        catch (UnauthorizedAccessException)
        {
            // SECURITY: Do not log exception details for auth failures - prevents info leakage
            _logger.LogWarning("Unauthorized access on {Method} {Path} from {IpAddress}",
                context.Request.Method, context.Request.Path,
                context.Connection.RemoteIpAddress);

            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                type = "Forbidden",
                error = "Access denied"
            }, JsonOptions));
        }
        catch (Exception ex)
        {
            // Generate a correlation ID so the error can be traced in logs without
            // exposing internal details to the client (OWASP A09:2021).
            var correlationId = Guid.NewGuid().ToString("N")[..12];

            _logger.LogError(ex, "Unhandled exception [{CorrelationId}] on {Method} {Path}",
                correlationId, context.Request.Method, context.Request.Path);

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            // SECURITY: Never return stack traces, exception types, or internal details.
            // Return only the correlation ID so support can look up the error in logs.
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                type = "InternalError",
                error = "An unexpected error occurred. If this persists, contact support.",
                correlationId
            }, JsonOptions));
        }
    }
}
