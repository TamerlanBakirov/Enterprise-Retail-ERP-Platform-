using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Notifications;

public class Notification : BaseEntity
{
    public Guid? UserId { get; private set; }
    public string Title { get; private set; } = default!;
    public string Message { get; private set; } = default!;
    public string NotificationType { get; private set; } = default!;
    public bool IsRead { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }

    private Notification() { }

    public static Notification Create(
        string title,
        string message,
        string notificationType,
        Guid? userId = null)
    {
        return new Notification
        {
            Title = title,
            Message = message,
            NotificationType = notificationType,
            UserId = userId,
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void MarkAsRead()
    {
        IsRead = true;
        ReadAt = DateTimeOffset.UtcNow;
    }
}
