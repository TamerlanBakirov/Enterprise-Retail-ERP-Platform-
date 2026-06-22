using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GeorgiaERP.Api.Hubs;

/// <summary>
/// SignalR hub for pushing real-time ERP notifications to connected clients.
/// Clients can subscribe to groups for targeted notifications (e.g., warehouse-specific alerts).
///
/// Client events:
///   - ReceiveNotification(eventType, payload) — main notification channel
///   - Connected(connectionId) — sent on successful connection
///
/// Client-to-server methods:
///   - JoinGroup(groupName) — subscribe to a notification group
///   - LeaveGroup(groupName) — unsubscribe from a notification group
/// </summary>
[Authorize]
public sealed class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        _logger.LogInformation("Client connected: {ConnectionId}, User: {UserId}", Context.ConnectionId, userId);

        // Auto-join user to their personal group
        if (userId is not null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        }

        // Auto-join user to role-based groups
        var roles = Context.User?.Claims
            .Where(c => c.Type == "roles")
            .Select(c => c.Value)
            .ToList();

        if (roles is not null)
        {
            foreach (var role in roles)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"role-{role}");
            }
        }

        await Clients.Caller.SendAsync("Connected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}, Reason: {Reason}",
            Context.ConnectionId, exception?.Message ?? "normal");
        return base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Allows a client to subscribe to a named notification group.
    /// Common groups: "warehouse-{id}", "store-{id}", "inventory-alerts".
    /// </summary>
    public async Task JoinGroup(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName) || groupName.Length > 100)
            return;

        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("Client {ConnectionId} joined group {Group}", Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Allows a client to unsubscribe from a named notification group.
    /// </summary>
    public async Task LeaveGroup(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("Client {ConnectionId} left group {Group}", Context.ConnectionId, groupName);
    }
}
