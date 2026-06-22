using GeorgiaERP.Domain.Compliance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Compliance;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.EntityType)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(a => a.EntityId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(a => a.Action)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.OldValues);
        builder.Property(a => a.NewValues);
        builder.Property(a => a.ChangedProperties);

        builder.Property(a => a.IpAddress)
            .HasMaxLength(45);

        builder.Property(a => a.Timestamp);

        builder.HasIndex(a => new { a.EntityType, a.EntityId })
            .HasDatabaseName("IX_audit_logs_entity");

        builder.HasIndex(a => a.Timestamp)
            .HasDatabaseName("IX_audit_logs_timestamp");

        builder.HasIndex(a => a.UserId)
            .HasDatabaseName("IX_audit_logs_user_id");
    }
}
