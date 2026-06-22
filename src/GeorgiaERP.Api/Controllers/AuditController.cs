using GeorgiaERP.Application.Audit.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeorgiaERP.Api.Controllers;

/// <summary>
/// Provides read-only access to the audit trail for compliance and investigation purposes.
/// Restricted to administrators.
/// </summary>
[Authorize(Roles = "super_admin,admin")]
[Tags("Audit")]
public class AuditController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public AuditController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Queries the audit log with optional filters by entity type, entity ID, user, and date range.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? entityType = null,
        [FromQuery] string? entityId = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _mediator.Send(new GetAuditLogsQuery(
            entityType, entityId, userId, from, to, page, pageSize));

        return Ok(result);
    }
}
