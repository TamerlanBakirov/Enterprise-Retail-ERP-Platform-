using System.Diagnostics;
using GeorgiaERP.Application.Backup;
using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GeorgiaERP.Infrastructure.Backup;

/// <summary>
/// PostgreSQL backup service that uses pg_dump and pg_restore for database operations.
/// Stores backup records in the application database and backup files on the local filesystem.
/// </summary>
public sealed class PostgresBackupService : IBackupService
{
    private readonly IAppDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PostgresBackupService> _logger;
    private readonly string _backupDirectory;

    public PostgresBackupService(
        IAppDbContext dbContext,
        IConfiguration configuration,
        ILogger<PostgresBackupService> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;

        _backupDirectory = configuration["Backup:Directory"]
            ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backups");
        Directory.CreateDirectory(_backupDirectory);
    }

    public string GetBackupDirectory() => _backupDirectory;

    public async Task<BackupRecord> CreateBackupAsync(
        BackupType type, Guid? userId, string? userName, string? notes, CancellationToken ct)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var fileName = $"erp_backup_{type.ToString().ToLower()}_{timestamp}.sql";
        var filePath = Path.Combine(_backupDirectory, fileName);

        var record = new BackupRecord
        {
            FileName = fileName,
            FilePath = filePath,
            Type = type,
            Status = BackupStatus.InProgress,
            InitiatedByUserId = userId,
            InitiatedByUserName = userName,
            Notes = notes,
            StartedAt = DateTimeOffset.UtcNow
        };

        _dbContext.BackupRecords.Add(record);
        await _dbContext.SaveChangesAsync(ct);

        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Database connection string not configured.");

            var pgArgs = ParseConnectionString(connectionString);
            var arguments = BuildPgDumpArguments(pgArgs, filePath, type);

            _logger.LogInformation("Starting database backup: {FileName}, Type: {Type}", fileName, type);

            var result = await RunProcessAsync("pg_dump", arguments, pgArgs.Password, ct);

            if (result.ExitCode == 0)
            {
                var fileInfo = new FileInfo(filePath);
                record.Status = BackupStatus.Completed;
                record.CompletedAt = DateTimeOffset.UtcNow;
                record.FileSizeBytes = fileInfo.Exists ? fileInfo.Length : 0;

                _logger.LogInformation("Backup completed successfully: {FileName}, Size: {Size} bytes",
                    fileName, record.FileSizeBytes);
            }
            else
            {
                record.Status = BackupStatus.Failed;
                record.ErrorMessage = result.StandardError;
                record.CompletedAt = DateTimeOffset.UtcNow;

                _logger.LogError("Backup failed: {FileName}, Error: {Error}", fileName, result.StandardError);
            }
        }
        catch (Exception ex)
        {
            record.Status = BackupStatus.Failed;
            record.ErrorMessage = ex.Message;
            record.CompletedAt = DateTimeOffset.UtcNow;

            _logger.LogError(ex, "Backup failed with exception: {FileName}", fileName);
        }

        await _dbContext.SaveChangesAsync(ct);
        return record;
    }

    public async Task<bool> RestoreBackupAsync(Guid backupId, CancellationToken ct)
    {
        var record = await _dbContext.BackupRecords.FindAsync(new object[] { backupId }, ct);
        if (record is null)
            throw new InvalidOperationException($"Backup record {backupId} not found.");

        if (!File.Exists(record.FilePath))
            throw new InvalidOperationException($"Backup file not found: {record.FilePath}");

        var connectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Database connection string not configured.");

        var pgArgs = ParseConnectionString(connectionString);
        var arguments = $"--host={pgArgs.Host} --port={pgArgs.Port} --username={pgArgs.Username} --dbname={pgArgs.Database} --file=\"{record.FilePath}\"";

        _logger.LogWarning("Starting database restore from backup: {BackupId}, File: {FileName}",
            backupId, record.FileName);

        var result = await RunProcessAsync("psql", arguments, pgArgs.Password, ct);

        if (result.ExitCode == 0)
        {
            _logger.LogInformation("Database restore completed successfully from: {FileName}", record.FileName);
            return true;
        }

        _logger.LogError("Database restore failed: {Error}", result.StandardError);
        return false;
    }

    public async Task<List<BackupRecord>> ListBackupsAsync(int page, int pageSize, CancellationToken ct)
    {
        return await _dbContext.BackupRecords.AsNoTracking()
            .OrderByDescending(b => b.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<bool> DeleteBackupAsync(Guid backupId, CancellationToken ct)
    {
        var record = await _dbContext.BackupRecords.FindAsync(new object[] { backupId }, ct);
        if (record is null) return false;

        // Delete the file if it exists
        if (File.Exists(record.FilePath))
        {
            try
            {
                File.Delete(record.FilePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete backup file: {FilePath}", record.FilePath);
            }
        }

        _dbContext.BackupRecords.Remove(record);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Deleted backup record: {BackupId}, File: {FileName}", backupId, record.FileName);
        return true;
    }

    private static string BuildPgDumpArguments(PgConnectionArgs pg, string filePath, BackupType type)
    {
        var args = $"--host={pg.Host} --port={pg.Port} --username={pg.Username} --format=plain --file=\"{filePath}\"";

        args += type switch
        {
            BackupType.SchemaOnly => " --schema-only",
            BackupType.DataOnly => " --data-only",
            _ => string.Empty
        };

        args += $" {pg.Database}";
        return args;
    }

    private static async Task<ProcessResult> RunProcessAsync(
        string fileName, string arguments, string? password, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // pg_dump/psql use PGPASSWORD env var for password
        if (!string.IsNullOrEmpty(password))
            psi.Environment["PGPASSWORD"] = password;

        using var process = new Process { StartInfo = psi };
        process.Start();

        var stdout = await process.StandardOutput.ReadToEndAsync(ct);
        var stderr = await process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);

        return new ProcessResult(process.ExitCode, stdout, stderr);
    }

    private static PgConnectionArgs ParseConnectionString(string connectionString)
    {
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0].Trim(), p => p[1].Trim(), StringComparer.OrdinalIgnoreCase);

        return new PgConnectionArgs(
            Host: parts.GetValueOrDefault("Host") ?? parts.GetValueOrDefault("Server") ?? "localhost",
            Port: parts.GetValueOrDefault("Port") ?? "5432",
            Database: parts.GetValueOrDefault("Database") ?? parts.GetValueOrDefault("Database Name") ?? "georgia_erp",
            Username: parts.GetValueOrDefault("Username") ?? parts.GetValueOrDefault("User Id") ?? "postgres",
            Password: parts.GetValueOrDefault("Password") ?? string.Empty);
    }

    private record PgConnectionArgs(string Host, string Port, string Database, string Username, string Password);
    private record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
