using System.Net;

namespace GeorgiaERP.Api.Middleware;

/// <summary>
/// Logs security-relevant events: failed authentication, permission denials,
/// rate-limit hits, and suspicious activity patterns (OWASP A09:2021 Security Logging).
/// </summary>
public class SecurityAuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityAuditMiddleware> _logger;

    public SecurityAuditMiddleware(RequestDelegate next, ILogger<SecurityAuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        var statusCode = context.Response.StatusCode;
        var path = context.Request.Path.Value;
        var method = context.Request.Method;
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        switch (statusCode)
        {
            case (int)HttpStatusCode.Unauthorized:
                _logger.LogWarning(
                    "SecurityAudit: Unauthorized access attempt. IP={IpAddress}, Method={Method}, Path={Path}, UserAgent={UserAgent}",
                    ip, method, path, GetSanitizedUserAgent(context));
                break;

            case (int)HttpStatusCode.Forbidden:
                var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
                _logger.LogWarning(
                    "SecurityAudit: Permission denied. UserId={UserId}, IP={IpAddress}, Method={Method}, Path={Path}",
                    userId, ip, method, path);
                break;

            case StatusCodes.Status429TooManyRequests:
                _logger.LogWarning(
                    "SecurityAudit: Rate limit exceeded. IP={IpAddress}, Method={Method}, Path={Path}",
                    ip, method, path);
                break;
        }
    }

    private static string GetSanitizedUserAgent(HttpContext context)
    {
        var ua = context.Request.Headers.UserAgent.ToString();
        // Truncate to prevent log injection with oversized User-Agent strings
        return ua.Length > 200 ? ua[..200] : ua;
    }
}
