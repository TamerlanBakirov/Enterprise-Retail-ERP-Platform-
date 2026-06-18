using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace GeorgiaERP.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    protected Guid CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : Guid.Empty;

    protected Guid? CurrentCompanyId =>
        Guid.TryParse(User.FindFirstValue("company_id"), out var id)
            ? id
            : null;

    protected string? CurrentIpAddress =>
        HttpContext.Connection.RemoteIpAddress?.ToString();
}
