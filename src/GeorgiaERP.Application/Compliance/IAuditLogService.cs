namespace GeorgiaERP.Application.Compliance;

public interface IAuditLogService
{
    void SetCurrentUser(Guid? userId, string? ipAddress);
}
