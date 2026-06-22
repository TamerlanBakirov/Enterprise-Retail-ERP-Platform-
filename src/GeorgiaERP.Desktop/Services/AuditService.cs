using GeorgiaERP.Desktop.Models;

namespace GeorgiaERP.Desktop.Services;

public interface IAuditService
{
    Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(
        string? entityType = null,
        string? entityId = null,
        Guid? userId = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        int page = 1,
        int pageSize = 50);
}

public class AuditService : IAuditService
{
    private readonly IApiClient _api;
    public AuditService(IApiClient api) => _api = api;

    public async Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(
        string? entityType, string? entityId, Guid? userId,
        DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize)
    {
        var parts = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrWhiteSpace(entityType)) parts.Add($"entityType={entityType}");
        if (!string.IsNullOrWhiteSpace(entityId)) parts.Add($"entityId={entityId}");
        if (userId.HasValue) parts.Add($"userId={userId}");
        if (from.HasValue) parts.Add($"from={from.Value:O}");
        if (to.HasValue) parts.Add($"to={to.Value:O}");

        var q = "audit?" + string.Join("&", parts);
        return await _api.GetAsync<PagedResult<AuditLogDto>>(q) ?? new PagedResult<AuditLogDto>();
    }
}
