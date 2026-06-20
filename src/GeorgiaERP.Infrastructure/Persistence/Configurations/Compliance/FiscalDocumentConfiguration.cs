using GeorgiaERP.Domain.Compliance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Compliance;

public class FiscalDocumentConfiguration : IEntityTypeConfiguration<FiscalDocument>
{
    public void Configure(EntityTypeBuilder<FiscalDocument> builder)
    {
        builder.ToTable("fiscal_documents");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.DocumentType)
            .HasConversion<string>();

        builder.Property(f => f.DocumentNumber)
            .HasMaxLength(100);

        builder.Property(f => f.InternalRef)
            .HasMaxLength(100);

        builder.Property(f => f.ReferenceType)
            .HasMaxLength(50);

        builder.Property(f => f.ReferenceId);

        builder.Property(f => f.Status)
            .HasConversion<string>();

        builder.Property(f => f.RsGeId)
            .HasMaxLength(50);

        builder.Property(f => f.RsGeStatus)
            .HasMaxLength(50);

        builder.Property(f => f.SubmissionDeadline);

        builder.Property(f => f.SubmittedAt);

        builder.Property(f => f.ConfirmedAt);

        builder.Property(f => f.RetryCount)
            .HasDefaultValue(0);

        builder.Property(f => f.LastError)
            .HasMaxLength(2000);

        builder.Property(f => f.DocumentData)
            .HasColumnType("jsonb");

        builder.Property(f => f.CreatedAt);

        builder.Property(f => f.UpdatedAt);

        builder.HasMany(f => f.CommunicationLogs)
            .WithOne(cl => cl.FiscalDocument)
            .HasForeignKey(cl => cl.FiscalDocumentId);

        // Document type + status composite for compliance dashboard
        builder.HasIndex(f => new { f.DocumentType, f.Status })
            .HasDatabaseName("IX_fiscal_documents_type_status");

        // Filtered index for pending/queued/failed documents requiring attention
        builder.HasIndex(f => f.Status)
            .HasDatabaseName("IX_fiscal_documents_pending_status")
            .HasFilter("\"Status\" IN ('PENDING', 'QUEUED', 'FAILED')");

        // Submission deadline tracking for compliance (30-day rule)
        builder.HasIndex(f => f.SubmissionDeadline)
            .HasDatabaseName("IX_fiscal_documents_deadline");

        // Reference document lookup (POS transaction, transfer order, etc.)
        builder.HasIndex(f => new { f.ReferenceType, f.ReferenceId })
            .HasDatabaseName("IX_fiscal_documents_reference");

        // RS.GE document ID for status sync
        builder.HasIndex(f => f.RsGeId)
            .HasDatabaseName("IX_fiscal_documents_rsge_id")
            .HasFilter("\"RsGeId\" IS NOT NULL");

        // Date ordering for fiscal document history
        builder.HasIndex(f => f.CreatedAt)
            .HasDatabaseName("IX_fiscal_documents_created_at");
    }
}
