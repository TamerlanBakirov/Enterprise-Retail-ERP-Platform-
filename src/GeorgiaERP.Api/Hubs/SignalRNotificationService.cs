using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace GeorgiaERP.Api.Hubs;

public class SignalRNotificationService : INotificationHub
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IAppDbContext _dbContext;

    public SignalRNotificationService(IHubContext<NotificationHub> hubContext, IAppDbContext dbContext)
    {
        _hubContext = hubContext;
        _dbContext = dbContext;
    }

    public async Task SendToUserAsync(Guid userId, string notificationType, object payload)
    {
        var notification = Notification.Create(notificationType, payload.ToString() ?? "", notificationType, userId);
        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();

        await _hubContext.Clients.Group($"user:{userId}")
            .SendAsync("ReceiveNotification", new { notificationType, payload, notificationId = notification.Id });
    }

    public async Task SendToRoleAsync(string role, string notificationType, object payload)
    {
        var notification = Notification.Create(notificationType, payload.ToString() ?? "", notificationType);
        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();

        await _hubContext.Clients.Group($"role:{role}")
            .SendAsync("ReceiveNotification", new { notificationType, payload, notificationId = notification.Id });
    }

    public async Task BroadcastAsync(string notificationType, object payload)
    {
        var notification = Notification.Create(notificationType, payload.ToString() ?? "", notificationType);
        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();

        await _hubContext.Clients.All
            .SendAsync("ReceiveNotification", new { notificationType, payload, notificationId = notification.Id });
    }
}
