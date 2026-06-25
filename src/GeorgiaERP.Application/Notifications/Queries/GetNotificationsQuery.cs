using GeorgiaERP.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Notifications.Queries;

public record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    string NotificationType,
    bool IsRead,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt);

public record GetNotificationsQuery(
    Guid UserId,
    int Page = 1,
    int PageSize = 20,
    bool? IsRead = null) : IRequest<PagedResult<NotificationDto>>;

public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, PagedResult<NotificationDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetNotificationsQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<PagedResult<NotificationDto>> Handle(GetNotificationsQuery request, CancellationToken ct)
    {
        var query = _dbContext.Notifications.AsNoTracking()
            .Where(n => n.UserId == request.UserId || n.UserId == null);

        if (request.IsRead.HasValue)
            query = query.Where(n => n.IsRead == request.IsRead.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(n => new NotificationDto(
                n.Id, n.Title, n.Message, n.NotificationType,
                n.IsRead, n.CreatedAt, n.ReadAt))
            .ToListAsync(ct);

        return new PagedResult<NotificationDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}

public record GetUnreadCountQuery(Guid UserId) : IRequest<int>;

public class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, int>
{
    private readonly IAppDbContext _dbContext;

    public GetUnreadCountQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<int> Handle(GetUnreadCountQuery request, CancellationToken ct)
    {
        return await _dbContext.Notifications
            .Where(n => (n.UserId == request.UserId || n.UserId == null) && !n.IsRead)
            .CountAsync(ct);
    }
}
