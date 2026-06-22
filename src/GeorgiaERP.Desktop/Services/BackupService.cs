using GeorgiaERP.Desktop.Models;

namespace GeorgiaERP.Desktop.Services;

public interface IBackupService
{
    Task<PagedResult<BackupRecordDto>> ListBackupsAsync(int page = 1, int pageSize = 20);
    Task<BackupRecordDto?> CreateBackupAsync(string type = "Full", string? notes = null);
    Task<ApiResult> RestoreBackupAsync(Guid backupId);
    Task<ApiResult> DeleteBackupAsync(Guid backupId);
}

public class BackupService : IBackupService
{
    private readonly IApiClient _api;
    public BackupService(IApiClient api) => _api = api;

    public async Task<PagedResult<BackupRecordDto>> ListBackupsAsync(int page, int pageSize)
        => await _api.GetAsync<PagedResult<BackupRecordDto>>($"backup?page={page}&pageSize={pageSize}")
           ?? new PagedResult<BackupRecordDto>();

    public Task<BackupRecordDto?> CreateBackupAsync(string type, string? notes)
        => _api.PostAsync<object, BackupRecordDto>("backup", new { Type = type, Notes = notes });

    public async Task<ApiResult> RestoreBackupAsync(Guid backupId)
        => await _api.PostAsync($"backup/{backupId}/restore");

    public async Task<ApiResult> DeleteBackupAsync(Guid backupId)
        => await _api.DeleteAsync($"backup/{backupId}");
}
