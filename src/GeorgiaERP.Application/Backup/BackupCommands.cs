using GeorgiaERP.Domain.Common;
using MediatR;

namespace GeorgiaERP.Application.Backup;

/// <summary>
/// Command to create a new database backup. Admin-only.
/// </summary>
public record CreateBackupCommand(
    BackupType Type = BackupType.Full,
    string? Notes = null,
    Guid? UserId = null,
    string? UserName = null) : IRequest<BackupRecord>;

/// <summary>
/// Command to restore database from a backup. Admin-only.
/// </summary>
public record RestoreBackupCommand(Guid BackupId) : IRequest<RestoreBackupResult>;

public record RestoreBackupResult(bool Success, string? ErrorMessage = null);

/// <summary>
/// Command to delete a backup record and its file. Admin-only.
/// </summary>
public record DeleteBackupCommand(Guid BackupId) : IRequest<bool>;

/// <summary>
/// Query to list backup history with pagination.
/// </summary>
public record ListBackupsQuery(int Page = 1, int PageSize = 20) : IRequest<BackupListResult>;

public record BackupListResult(
    List<BackupRecordDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record BackupRecordDto(
    Guid Id,
    string FileName,
    long FileSizeBytes,
    string Type,
    string Status,
    string? ErrorMessage,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    string? InitiatedByUserName,
    string? Notes);

// --- Handlers ---

public class CreateBackupCommandHandler : IRequestHandler<CreateBackupCommand, BackupRecord>
{
    private readonly IBackupService _backupService;

    public CreateBackupCommandHandler(IBackupService backupService) => _backupService = backupService;

    public Task<BackupRecord> Handle(CreateBackupCommand request, CancellationToken ct)
    {
        return _backupService.CreateBackupAsync(
            request.Type,
            request.UserId,
            request.UserName,
            request.Notes,
            ct);
    }
}

public class RestoreBackupCommandHandler : IRequestHandler<RestoreBackupCommand, RestoreBackupResult>
{
    private readonly IBackupService _backupService;

    public RestoreBackupCommandHandler(IBackupService backupService) => _backupService = backupService;

    public async Task<RestoreBackupResult> Handle(RestoreBackupCommand request, CancellationToken ct)
    {
        try
        {
            var success = await _backupService.RestoreBackupAsync(request.BackupId, ct);
            return new RestoreBackupResult(success, success ? null : "Restore operation failed.");
        }
        catch (Exception ex)
        {
            return new RestoreBackupResult(false, ex.Message);
        }
    }
}

public class DeleteBackupCommandHandler : IRequestHandler<DeleteBackupCommand, bool>
{
    private readonly IBackupService _backupService;
    public DeleteBackupCommandHandler(IBackupService backupService) => _backupService = backupService;

    public Task<bool> Handle(DeleteBackupCommand request, CancellationToken ct)
        => _backupService.DeleteBackupAsync(request.BackupId, ct);
}

public class ListBackupsQueryHandler : IRequestHandler<ListBackupsQuery, BackupListResult>
{
    private readonly IBackupService _backupService;
    public ListBackupsQueryHandler(IBackupService backupService) => _backupService = backupService;

    public async Task<BackupListResult> Handle(ListBackupsQuery request, CancellationToken ct)
    {
        var records = await _backupService.ListBackupsAsync(request.Page, request.PageSize, ct);
        var dtos = records.Select(r => new BackupRecordDto(
            r.Id,
            r.FileName,
            r.FileSizeBytes,
            r.Type.ToString(),
            r.Status.ToString(),
            r.ErrorMessage,
            r.StartedAt,
            r.CompletedAt,
            r.InitiatedByUserName,
            r.Notes)).ToList();

        return new BackupListResult(dtos, dtos.Count, request.Page, request.PageSize);
    }
}
