namespace GeorgiaERP.Application.Common;

public interface INotificationHub
{
    Task SendToUserAsync(Guid userId, string notificationType, object payload);
    Task SendToRoleAsync(string role, string notificationType, object payload);
    Task BroadcastAsync(string notificationType, object payload);
}
