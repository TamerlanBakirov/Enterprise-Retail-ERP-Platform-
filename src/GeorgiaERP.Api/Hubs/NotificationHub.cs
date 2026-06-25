using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GeorgiaERP.Api.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly IAppDbContext _dbContext;

    public NotificationHub(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is not null)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");

        var roles = Context.User?.FindAll(ClaimTypes.Role).Select(c => c.Value) ?? [];
        foreach (var role in roles)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"role:{role}");

        await base.OnConnectedAsync();
    }

    public async Task MarkAsRead(Guid notificationId)
    {
        var notification = await _dbContext.Notifications.FindAsync(notificationId);
        if (notification is not null)
        {
            notification.MarkAsRead();
            await _dbContext.SaveChangesAsync();
        }
    }
}
