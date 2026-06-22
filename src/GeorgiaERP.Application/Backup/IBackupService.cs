using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Application.Backup;

/// <summary>
/// Abstraction for database backup and restore operations.
/// Implementations generate pg_dump/pg_restore commands for PostgreSQL.
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Creates a database backup and returns the backup record.
    /// </summary>
    Task<BackupRecord> CreateBackupAsync(BackupType type, Guid? userId, string? userName, string? notes = null, CancellationToken ct = default);

    /// <summary>
    /// Restores the database from a backup file.
    /// </summary>
    Task<bool> RestoreBackupAsync(Guid backupId, CancellationToken ct = default);

    /// <summary>
    /// Lists all backup records with optional filtering.
    /// </summary>
    Task<List<BackupRecord>> ListBackupsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);

    /// <summary>
    /// Deletes a backup record and its associated file.
    /// </summary>
    Task<bool> DeleteBackupAsync(Guid backupId, CancellationToken ct = default);

    /// <summary>
    /// Gets the backup storage directory path.
    /// </summary>
    string GetBackupDirectory();
}
