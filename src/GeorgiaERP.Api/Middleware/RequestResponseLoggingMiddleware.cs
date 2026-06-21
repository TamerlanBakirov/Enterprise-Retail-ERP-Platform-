using System.Diagnostics;
using System.Text;

namespace GeorgiaERP.Api.Middleware;

/// <summary>
/// Logs sanitized request and response metadata for diagnostics.
/// SECURITY: Request/response bodies are NOT logged to prevent PII and
/// credential leakage. Only method, path, status code, duration, and
/// content length are recorded. Authorization headers are redacted.
/// </summary>
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    /// <summary>Headers whose values must be redacted in log output.</summary>
    private static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Cookie",
        "Set-Cookie",
        "X-Api-Key"
    };

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        // Skip logging for high-frequency infrastructure endpoints.
        if (path.StartsWith("/metrics", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var correlationId = context.Items["CorrelationId"]?.ToString() ?? "none";

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            var sanitizedHeaders = SanitizeHeaders(context.Request.Headers);
            _logger.LogDebug(
                "HTTP Request [{CorrelationId}] {Method} {Path}{QueryString} ContentLength={ContentLength} Headers={@Headers}",
                correlationId,
                context.Request.Method,
                path,
                context.Request.QueryString.Value,
                context.Request.ContentLength,
                sanitizedHeaders);
        }

        var stopwatch = Stopwatch.StartNew();
        await _next(context);
        stopwatch.Stop();

        _logger.LogInformation(
            "HTTP Response [{CorrelationId}] {Method} {Path} => {StatusCode} in {ElapsedMs}ms",
            correlationId,
            context.Request.Method,
            path,
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds);
    }

    private static Dictionary<string, string> SanitizeHeaders(IHeaderDictionary headers)
    {
        var sanitized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in headers)
        {
            sanitized[header.Key] = SensitiveHeaders.Contains(header.Key)
                ? "[REDACTED]"
                : header.Value.ToString();
        }
        return sanitized;
    }
}
