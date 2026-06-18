namespace GeorgiaERP.Domain.Common;

public abstract class AuditableEntity : BaseEntity, IAuditableEntity
{
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid UpdatedBy { get; private set; }

    protected AuditableEntity() : base() { }

    protected AuditableEntity(Guid id) : base(id) { }
}
