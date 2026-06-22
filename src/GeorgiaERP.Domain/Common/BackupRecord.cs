namespace GeorgiaERP.Domain.Common;

/// <summary>
/// Tracks database backup operations and their results.
/// </summary>
public class BackupRecord : BaseEntity
{
    public required string FileName { get; set; }
    public required string FilePath { get; set; }
    public long FileSizeBytes { get; set; }
    public BackupType Type { get; set; } = BackupType.Full;
    public BackupStatus Status { get; set; } = BackupStatus.InProgress;
    public string? ErrorMessage { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public Guid? InitiatedByUserId { get; set; }
    public string? InitiatedByUserName { get; set; }
    public string? Notes { get; set; }
}

public enum BackupType
{
    Full,
    SchemaOnly,
    DataOnly
}

public enum BackupStatus
{
    InProgress,
    Completed,
    Failed
}
