using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Compliance.DTOs;
using MediatR;

namespace GeorgiaERP.Application.Compliance.Queries;

public record GetAuditLogsQuery(
    int Page = 1,
    int PageSize = 20,
    string? EntityType = null,
    string? EntityId = null,
    Guid? UserId = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null) : IRequest<PagedResult<AuditLogDto>>;
