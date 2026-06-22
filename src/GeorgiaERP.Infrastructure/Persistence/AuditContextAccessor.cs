using GeorgiaERP.Application.Common;

namespace GeorgiaERP.Infrastructure.Persistence;

public class AuditContextAccessor : IAuditContextAccessor
{
    public Guid? UserId { get; set; }
    public string? IpAddress { get; set; }
}
