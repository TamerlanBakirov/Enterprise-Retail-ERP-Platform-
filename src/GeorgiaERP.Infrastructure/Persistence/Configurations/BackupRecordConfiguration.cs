using GeorgiaERP.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations;

public class BackupRecordConfiguration : IEntityTypeConfiguration<BackupRecord>
{
    public void Configure(EntityTypeBuilder<BackupRecord> builder)
    {
        builder.ToTable("backup_records");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.FileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(b => b.FilePath)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(b => b.Type)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(b => b.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(b => b.InitiatedByUserName)
            .HasMaxLength(200);

        builder.Property(b => b.Notes)
            .HasMaxLength(500);

        builder.HasIndex(b => b.StartedAt);
        builder.HasIndex(b => b.Status);
    }
}
