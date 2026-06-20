namespace GeorgiaERP.Domain.Common;

public interface ISoftDeletable
{
    DateTimeOffset? DeletedAt { get; }
}
