using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Webhooks.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Webhooks.Queries;

public record GetWebhooksQuery(bool? IsActive = null) : IRequest<IReadOnlyList<WebhookSubscriptionDto>>;

public record GetWebhookByIdQuery(Guid Id) : IRequest<WebhookSubscriptionDto?>;

public record GetWebhookDeliveryLogsQuery(Guid SubscriptionId, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<WebhookDeliveryLogDto>>;

public class GetWebhooksQueryHandler : IRequestHandler<GetWebhooksQuery, IReadOnlyList<WebhookSubscriptionDto>>
{
    private readonly IAppDbContext _db;

    public GetWebhooksQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<WebhookSubscriptionDto>> Handle(GetWebhooksQuery request, CancellationToken cancellationToken)
    {
        var query = _db.WebhookSubscriptions.AsNoTracking();

        if (request.IsActive.HasValue)
            query = query.Where(w => w.IsActive == request.IsActive.Value);

        var entities = await query.OrderBy(w => w.Name).ToListAsync(cancellationToken);

        return entities.Select(w => new WebhookSubscriptionDto(
            w.Id, w.Name, w.Url, w.GetEventTypes(),
            w.IsActive, w.MaxRetries, w.ConsecutiveFailures,
            w.LastDeliveryAt, w.LastDeliveryStatus, w.CreatedAt)).ToList();
    }
}

public class GetWebhookByIdQueryHandler : IRequestHandler<GetWebhookByIdQuery, WebhookSubscriptionDto?>
{
    private readonly IAppDbContext _db;

    public GetWebhookByIdQueryHandler(IAppDbContext db) => _db = db;

    public async Task<WebhookSubscriptionDto?> Handle(GetWebhookByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _db.WebhookSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken);

        if (entity is null) return null;

        return new WebhookSubscriptionDto(
            entity.Id, entity.Name, entity.Url, entity.GetEventTypes(),
            entity.IsActive, entity.MaxRetries, entity.ConsecutiveFailures,
            entity.LastDeliveryAt, entity.LastDeliveryStatus, entity.CreatedAt);
    }
}

public class GetWebhookDeliveryLogsQueryHandler
    : IRequestHandler<GetWebhookDeliveryLogsQuery, PagedResult<WebhookDeliveryLogDto>>
{
    private readonly IAppDbContext _db;

    public GetWebhookDeliveryLogsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PagedResult<WebhookDeliveryLogDto>> Handle(
        GetWebhookDeliveryLogsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.WebhookDeliveryLogs
            .AsNoTracking()
            .Where(d => d.SubscriptionId == request.SubscriptionId);

        var totalCount = await query.CountAsync(cancellationToken);

        // Use Id for ordering since SQLite does not support DateTimeOffset in ORDER BY.
        // UUIDs are generated sequentially enough for recent entries, and this avoids
        // provider-specific LINQ translation issues.
        var entities = await query
            .OrderByDescending(d => d.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = entities
            .OrderByDescending(d => d.AttemptedAt)
            .Select(d => new WebhookDeliveryLogDto(
                d.Id, d.SubscriptionId, d.EventType, d.AttemptNumber,
                d.Success, d.HttpStatusCode, d.ErrorMessage, d.AttemptedAt, d.DurationMs))
            .ToList();

        return new PagedResult<WebhookDeliveryLogDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
