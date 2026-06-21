using GeorgiaERP.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.EntityType).HasMaxLength(128).IsRequired();
        builder.Property(a => a.EntityId).HasMaxLength(64).IsRequired();
        builder.Property(a => a.Action).HasMaxLength(16).IsRequired();
        builder.Property(a => a.ChangedProperties).HasColumnType("text");
        builder.Property(a => a.IpAddress).HasMaxLength(45);
        builder.Property(a => a.Timestamp).IsRequired();

        // Index for querying by entity
        builder.HasIndex(a => new { a.EntityType, a.EntityId });

        // Index for querying by time range
        builder.HasIndex(a => a.Timestamp);

        // Index for querying by user
        builder.HasIndex(a => a.UserId);
    }
}
