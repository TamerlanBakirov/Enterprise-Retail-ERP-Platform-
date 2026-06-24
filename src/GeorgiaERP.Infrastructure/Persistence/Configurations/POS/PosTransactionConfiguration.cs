using GeorgiaERP.Domain.POS;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.POS;

public class PosTransactionConfiguration : IEntityTypeConfiguration<PosTransaction>
{
    public void Configure(EntityTypeBuilder<PosTransaction> builder)
    {
        builder.ToTable("pos_transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TransactionNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(t => t.TransactionNumber)
            .IsUnique();

        builder.Property(t => t.SessionId);

        builder.Property(t => t.StoreId);

        builder.Property(t => t.CustomerId);

        builder.Property(t => t.TransactionType)
            .HasConversion<string>();

        builder.Property(t => t.Subtotal)
            .HasPrecision(18, 2);

        builder.Property(t => t.DiscountTotal)
            .HasPrecision(18, 2);

        builder.Property(t => t.VatTotal)
            .HasPrecision(18, 2);

        builder.Property(t => t.Total)
            .HasPrecision(18, 2);

        builder.Property(t => t.Status)
            .HasConversion<string>();

        builder.Property(t => t.FiscalReceiptId)
            .HasMaxLength(100);

        builder.Property(t => t.CreatedAt);

        builder.Property(t => t.CreatedBy);

        builder.Property(t => t.VoidedAt);

        builder.Property(t => t.VoidedBy);

        builder.Property(t => t.VoidReason)
            .HasMaxLength(500);

        builder.Property(t => t.OriginalTransactionId);

        // Returns reference their original sale; index for return-lookup per sale.
        builder.HasIndex(t => t.OriginalTransactionId)
            .HasDatabaseName("IX_pos_transactions_original");

        builder.HasOne(t => t.Session)
            .WithMany(s => s.Transactions)
            .HasForeignKey(t => t.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Lines)
            .WithOne(l => l.Transaction)
            .HasForeignKey(l => l.TransactionId);

        builder.HasMany(t => t.Payments)
            .WithOne(p => p.Transaction)
            .HasForeignKey(p => p.TransactionId);

        // Composite index for store-level date queries (sales reports, daily closings)
        builder.HasIndex(t => new { t.StoreId, t.CreatedAt })
            .HasDatabaseName("IX_pos_transactions_store_date");

        // Session lookup - high-frequency join from POS session detail views
        builder.HasIndex(t => t.SessionId)
            .HasDatabaseName("IX_pos_transactions_session");

        // Status filter - used for pending/voided transaction queries
        builder.HasIndex(t => t.Status)
            .HasDatabaseName("IX_pos_transactions_status");

        // Date range queries - sales reports, compliance reporting
        builder.HasIndex(t => t.CreatedAt)
            .HasDatabaseName("IX_pos_transactions_created_at");

        // Customer purchase history lookups
        builder.HasIndex(t => t.CustomerId)
            .HasDatabaseName("IX_pos_transactions_customer")
            .HasFilter("\"CustomerId\" IS NOT NULL");
    }
}
