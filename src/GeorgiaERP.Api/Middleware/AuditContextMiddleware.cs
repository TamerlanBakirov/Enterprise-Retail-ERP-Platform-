using System.Security.Claims;
using GeorgiaERP.Application.Common;

namespace GeorgiaERP.Api.Middleware;

public class AuditContextMiddleware
{
    private readonly RequestDelegate _next;

    public AuditContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAuditContextAccessor auditContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            if (Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) && userId != Guid.Empty)
                auditContext.UserId = userId;
        }

        auditContext.IpAddress = context.Connection.RemoteIpAddress?.ToString();

        await _next(context);
    }
}
