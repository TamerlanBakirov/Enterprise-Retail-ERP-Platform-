using GeorgiaERP.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Audit.Queries;

public record GetAuditLogsQuery(
    string? EntityType = null,
    string? EntityId = null,
    Guid? UserId = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedResult<AuditLogDto>>;

public record AuditLogDto(
    Guid Id,
    string EntityType,
    string EntityId,
    string Action,
    string? ChangedProperties,
    Guid? UserId,
    DateTimeOffset Timestamp);

public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetAuditLogsQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<PagedResult<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken ct)
    {
        var query = _dbContext.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.EntityType))
            query = query.Where(a => a.EntityType == request.EntityType);

        if (!string.IsNullOrWhiteSpace(request.EntityId))
            query = query.Where(a => a.EntityId == request.EntityId);

        if (request.UserId.HasValue)
            query = query.Where(a => a.UserId == request.UserId.Value);

        if (request.From.HasValue)
            query = query.Where(a => a.Timestamp >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(a => a.Timestamp <= request.To.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new AuditLogDto(
                a.Id,
                a.EntityType,
                a.EntityId,
                a.Action,
                a.ChangedProperties,
                a.UserId,
                a.Timestamp))
            .ToListAsync(ct);

        return new PagedResult<AuditLogDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
