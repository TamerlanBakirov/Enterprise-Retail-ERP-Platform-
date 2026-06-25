using GeorgiaERP.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Payments;

public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("payment_transactions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.OrderId);

        builder.Property(p => p.Amount)
            .HasPrecision(18, 2);

        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(p => p.Provider)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.ExternalTransactionId)
            .HasMaxLength(200);

        builder.Property(p => p.ErrorMessage)
            .HasMaxLength(1000);

        builder.Property(p => p.Metadata)
            .HasMaxLength(4000);

        builder.Property(p => p.CreatedAt);
        builder.Property(p => p.CompletedAt);

        builder.HasIndex(p => p.OrderId)
            .HasDatabaseName("IX_payment_transactions_order");

        builder.HasIndex(p => p.ExternalTransactionId)
            .HasDatabaseName("IX_payment_transactions_external_id")
            .HasFilter("\"ExternalTransactionId\" IS NOT NULL");

        builder.HasIndex(p => p.Status)
            .HasDatabaseName("IX_payment_transactions_status");
    }
}
