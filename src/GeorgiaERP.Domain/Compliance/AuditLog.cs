namespace GeorgiaERP.Domain.Compliance;

public enum AuditAction { Create, Update, Delete }

public class AuditLog
{
    public Guid Id { get; private set; }
    public string EntityType { get; private set; } = default!;
    public string EntityId { get; private set; } = default!;
    public AuditAction Action { get; private set; }
    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }
    public string? ChangedProperties { get; private set; }
    public Guid? UserId { get; private set; }
    public string? IpAddress { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(
        string entityType,
        string entityId,
        AuditAction action,
        string? oldValues,
        string? newValues,
        string? changedProperties,
        Guid? userId,
        string? ipAddress)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            OldValues = oldValues,
            NewValues = newValues,
            ChangedProperties = changedProperties,
            UserId = userId,
            IpAddress = ipAddress,
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}
