using GeorgiaERP.Domain.CRM;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.CRM;

public class LoyaltyTransactionConfiguration : IEntityTypeConfiguration<LoyaltyTransaction>
{
    public void Configure(EntityTypeBuilder<LoyaltyTransaction> builder)
    {
        builder.ToTable("loyalty_transactions");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.CustomerId);

        builder.Property(l => l.TransactionType)
            .HasConversion<string>();

        builder.Property(l => l.Points);

        builder.Property(l => l.ReferenceType)
            .HasMaxLength(50);

        builder.Property(l => l.ReferenceId);

        builder.Property(l => l.Description)
            .HasMaxLength(500);

        builder.Property(l => l.BalanceAfter);

        builder.Property(l => l.CreatedAt);

        builder.HasOne(l => l.Customer)
            .WithMany(c => c.LoyaltyTransactions)
            .HasForeignKey(l => l.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Composite for customer loyalty history ordered by date
        builder.HasIndex(l => new { l.CustomerId, l.CreatedAt })
            .HasDatabaseName("IX_loyalty_transactions_customer_date");

        // Transaction type filtering (EARN, REDEEM, etc.)
        builder.HasIndex(l => l.TransactionType)
            .HasDatabaseName("IX_loyalty_transactions_type");
    }
}
