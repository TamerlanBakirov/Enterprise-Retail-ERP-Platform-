namespace GeorgiaERP.Domain.Common;

/// <summary>
/// Immutable audit trail entry capturing who changed what, when, and how.
/// Written automatically by the SaveChanges interceptor for entities
/// implementing <see cref="IAuditableEntity"/>.
/// </summary>
public class AuditLog
{
    public Guid Id { get; private set; }
    public string EntityType { get; private set; } = default!;
    public string EntityId { get; private set; } = default!;
    public string Action { get; private set; } = default!; // Created, Updated, Deleted
    public string? ChangedProperties { get; private set; } // JSON: {"Name": {"Old": "x", "New": "y"}}
    public Guid? UserId { get; private set; }
    public string? IpAddress { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(
        string entityType,
        string entityId,
        string action,
        string? changedProperties = null,
        Guid? userId = null,
        string? ipAddress = null)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            ChangedProperties = changedProperties,
            UserId = userId,
            IpAddress = ipAddress,
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}
