namespace GeorgiaERP.Api.Middleware;

/// <summary>
/// Enforces a maximum request body size to prevent denial-of-service via
/// oversized payloads. ASP.NET Core has its own limits, but this middleware
/// provides an explicit, auditable guard (OWASP A05:2021 Security Misconfiguration).
/// </summary>
public class RequestSizeLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestSizeLimitMiddleware> _logger;

    // 10 MB default - suitable for an ERP API (no file uploads on most endpoints)
    private const long MaxRequestBodySize = 10 * 1024 * 1024;

    public RequestSizeLimitMiddleware(RequestDelegate next, ILogger<RequestSizeLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.ContentLength > MaxRequestBodySize)
        {
            _logger.LogWarning(
                "SecurityAudit: Request body too large. IP={IpAddress}, Size={ContentLength}, Path={Path}",
                context.Connection.RemoteIpAddress, context.Request.ContentLength, context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            await context.Response.WriteAsync("Request body exceeds the maximum allowed size.");
            return;
        }

        await _next(context);
    }
}
