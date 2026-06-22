using GeorgiaERP.Application.Compliance.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeorgiaERP.Api.Controllers;

[Authorize]
[Route("api/v1/audit-logs")]
public class AuditController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public AuditController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? entityType = null,
        [FromQuery] string? entityId = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null)
    {
        var query = new GetAuditLogsQuery(page, pageSize, entityType, entityId, userId, from, to);
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
