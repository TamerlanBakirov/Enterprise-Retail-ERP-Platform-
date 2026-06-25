using GeorgiaERP.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Notifications.Commands;

public record MarkNotificationReadCommand(Guid NotificationId) : IRequest<Result>;

public class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, Result>
{
    private readonly IAppDbContext _dbContext;

    public MarkNotificationReadCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken ct)
    {
        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId, ct);

        if (notification is null)
            return Result.NotFound("Notification", request.NotificationId);

        notification.MarkAsRead();
        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public record MarkAllNotificationsReadCommand(Guid UserId) : IRequest<Result>;

public class MarkAllNotificationsReadCommandHandler : IRequestHandler<MarkAllNotificationsReadCommand, Result>
{
    private readonly IAppDbContext _dbContext;

    public MarkAllNotificationsReadCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(MarkAllNotificationsReadCommand request, CancellationToken ct)
    {
        var unread = await _dbContext.Notifications
            .Where(n => (n.UserId == request.UserId || n.UserId == null) && !n.IsRead)
            .ToListAsync(ct);

        foreach (var n in unread)
            n.MarkAsRead();

        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}
