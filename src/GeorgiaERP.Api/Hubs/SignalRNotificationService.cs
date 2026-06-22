using GeorgiaERP.Application.Common;
using Microsoft.AspNetCore.SignalR;

namespace GeorgiaERP.Api.Hubs;

/// <summary>
/// SignalR implementation of <see cref="INotificationService"/>.
/// Dispatches real-time events via the <see cref="NotificationHub"/>.
/// </summary>
public sealed class SignalRNotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<SignalRNotificationService> _logger;

    public SignalRNotificationService(
        IHubContext<NotificationHub> hubContext,
        ILogger<SignalRNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendToAllAsync(string eventType, object payload, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Broadcasting notification: {EventType}", eventType);
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", eventType, payload, cancellationToken);
    }

    public async Task SendToUserAsync(Guid userId, string eventType, object payload, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Sending notification to user {UserId}: {EventType}", userId, eventType);
        await _hubContext.Clients.Group($"user-{userId}").SendAsync("ReceiveNotification", eventType, payload, cancellationToken);
    }

    public async Task SendToGroupAsync(string group, string eventType, object payload, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Sending notification to group {Group}: {EventType}", group, eventType);
        await _hubContext.Clients.Group(group).SendAsync("ReceiveNotification", eventType, payload, cancellationToken);
    }
}
