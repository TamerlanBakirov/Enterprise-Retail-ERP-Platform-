using Serilog.Context;

namespace GeorgiaERP.Api.Middleware;

/// <summary>
/// Ensures every request has a correlation ID for distributed tracing.
/// If the client sends an X-Correlation-ID header, that value is used;
/// otherwise a new GUID is generated. The correlation ID is:
///   1. Pushed into the Serilog LogContext so all downstream log entries include it.
///   2. Added to the response headers so the client can reference it in support requests.
///   3. Stored in HttpContext.Items for in-process access.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        context.Items["CorrelationId"] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        // Push into Serilog LogContext so every log entry in this request scope
        // includes the CorrelationId property automatically.
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
