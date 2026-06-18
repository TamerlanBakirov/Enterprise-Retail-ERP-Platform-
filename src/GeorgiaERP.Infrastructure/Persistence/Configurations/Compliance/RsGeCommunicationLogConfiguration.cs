using GeorgiaERP.Domain.Compliance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Compliance;

public class RsGeCommunicationLogConfiguration : IEntityTypeConfiguration<RsGeCommunicationLog>
{
    public void Configure(EntityTypeBuilder<RsGeCommunicationLog> builder)
    {
        builder.ToTable("rsge_communication_logs");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.FiscalDocumentId);

        builder.Property(c => c.Operation)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Direction)
            .HasConversion<string>();

        builder.Property(c => c.Endpoint)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(c => c.RequestPayload);

        builder.Property(c => c.ResponsePayload);

        builder.Property(c => c.HttpStatus);

        builder.Property(c => c.DurationMs);

        builder.Property(c => c.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(c => c.CorrelationId);

        builder.HasIndex(c => c.CorrelationId);

        builder.Property(c => c.CreatedAt);

        builder.HasOne(c => c.FiscalDocument)
            .WithMany(fd => fd.CommunicationLogs)
            .HasForeignKey(c => c.FiscalDocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
