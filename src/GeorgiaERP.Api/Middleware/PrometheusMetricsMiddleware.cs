using System.Diagnostics;
using GeorgiaERP.Api.Monitoring;

namespace GeorgiaERP.Api.Middleware;

/// <summary>
/// Records HTTP request duration and count metrics for Prometheus.
/// Placed early in the pipeline so it captures the full request lifecycle
/// including exception handling and authentication overhead.
/// </summary>
public class PrometheusMetricsMiddleware
{
    private readonly RequestDelegate _next;

    public PrometheusMetricsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip metrics and health check endpoints to avoid self-referential noise.
        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/metrics", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var method = context.Request.Method;
            var endpoint = NormalizeEndpoint(context);
            var statusCode = context.Response.StatusCode.ToString();

            ErpMetrics.HttpRequestDuration
                .WithLabels(method, endpoint, statusCode)
                .Observe(stopwatch.Elapsed.TotalSeconds);

            ErpMetrics.HttpRequestTotal
                .WithLabels(method, endpoint, statusCode)
                .Inc();
        }
    }

    /// <summary>
    /// Normalizes the request path by replacing numeric path segments with
    /// placeholders to keep cardinality manageable. For example,
    /// /api/v1/products/42 becomes /api/v1/products/{id}.
    /// </summary>
    private static string NormalizeEndpoint(HttpContext context)
    {
        // Prefer the route pattern from endpoint routing when available.
        var endpoint = context.GetEndpoint();
        if (endpoint is RouteEndpoint routeEndpoint)
        {
            return routeEndpoint.RoutePattern.RawText ?? context.Request.Path.Value ?? "/";
        }

        // Fallback: replace numeric segments with {id}.
        var path = context.Request.Path.Value ?? "/";
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < segments.Length; i++)
        {
            if (Guid.TryParse(segments[i], out _) || long.TryParse(segments[i], out _))
            {
                segments[i] = "{id}";
            }
        }

        return "/" + string.Join("/", segments);
    }
}
