namespace GeorgiaERP.Domain.Common;

public interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; }
    DateTimeOffset UpdatedAt { get; }
    Guid CreatedBy { get; }
    Guid UpdatedBy { get; }
}
