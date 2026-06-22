namespace GeorgiaERP.Application.Common;

public interface IAuditContextAccessor
{
    Guid? UserId { get; set; }
    string? IpAddress { get; set; }
}
