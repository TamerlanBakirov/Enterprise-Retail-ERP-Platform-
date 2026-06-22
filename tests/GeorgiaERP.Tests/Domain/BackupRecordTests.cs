using FluentAssertions;
using GeorgiaERP.Domain.Common;
using Xunit;

namespace GeorgiaERP.Tests.Domain;

public class BackupRecordTests
{
    [Fact]
    public void BackupRecord_DefaultsToInProgressStatus()
    {
        var record = new BackupRecord
        {
            FileName = "test_backup.sql",
            FilePath = "/backups/test_backup.sql"
        };

        record.Status.Should().Be(BackupStatus.InProgress);
        record.Type.Should().Be(BackupType.Full);
        record.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void BackupRecord_CanTransitionToCompleted()
    {
        var record = new BackupRecord
        {
            FileName = "test_backup.sql",
            FilePath = "/backups/test_backup.sql"
        };

        record.Status = BackupStatus.Completed;
        record.CompletedAt = DateTimeOffset.UtcNow;
        record.FileSizeBytes = 1024 * 1024;

        record.Status.Should().Be(BackupStatus.Completed);
        record.CompletedAt.Should().NotBeNull();
        record.FileSizeBytes.Should().Be(1024 * 1024);
    }

    [Fact]
    public void BackupRecord_CanTransitionToFailed()
    {
        var record = new BackupRecord
        {
            FileName = "test_backup.sql",
            FilePath = "/backups/test_backup.sql"
        };

        record.Status = BackupStatus.Failed;
        record.ErrorMessage = "pg_dump not found";
        record.CompletedAt = DateTimeOffset.UtcNow;

        record.Status.Should().Be(BackupStatus.Failed);
        record.ErrorMessage.Should().Be("pg_dump not found");
    }

    [Theory]
    [InlineData(BackupType.Full)]
    [InlineData(BackupType.SchemaOnly)]
    [InlineData(BackupType.DataOnly)]
    public void BackupRecord_SupportsAllBackupTypes(BackupType type)
    {
        var record = new BackupRecord
        {
            FileName = $"backup_{type}.sql",
            FilePath = $"/backups/backup_{type}.sql",
            Type = type
        };

        record.Type.Should().Be(type);
    }

    [Fact]
    public void BackupRecord_TracksInitiator()
    {
        var userId = Guid.NewGuid();
        var record = new BackupRecord
        {
            FileName = "test_backup.sql",
            FilePath = "/backups/test_backup.sql",
            InitiatedByUserId = userId,
            InitiatedByUserName = "admin",
            Notes = "Scheduled daily backup"
        };

        record.InitiatedByUserId.Should().Be(userId);
        record.InitiatedByUserName.Should().Be("admin");
        record.Notes.Should().Be("Scheduled daily backup");
    }
}
