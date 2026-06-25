using GeorgiaERP.Domain.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Reports;

public class ScheduledReportConfiguration : IEntityTypeConfiguration<ScheduledReport>
{
    public void Configure(EntityTypeBuilder<ScheduledReport> builder)
    {
        builder.ToTable("scheduled_reports");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.ReportType)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(r => r.CronExpression)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.Recipients)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(r => r.Format)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(r => r.IsActive)
            .HasDefaultValue(true);

        builder.Property(r => r.CreatedAt);
        builder.Property(r => r.LastRunAt);
        builder.Property(r => r.NextRunAt);
        builder.Property(r => r.CreatedBy);

        builder.HasIndex(r => r.IsActive)
            .HasDatabaseName("IX_scheduled_reports_active");

        builder.HasIndex(r => r.Name)
            .IsUnique()
            .HasDatabaseName("IX_scheduled_reports_name");
    }
}
