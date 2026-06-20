using GeorgiaERP.Domain.POS;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.POS;

public class DailyClosingConfiguration : IEntityTypeConfiguration<DailyClosing>
{
    public void Configure(EntityTypeBuilder<DailyClosing> builder)
    {
        builder.ToTable("daily_closings");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.StoreId);

        builder.Property(d => d.ClosingDate);

        builder.Property(d => d.TotalSales)
            .HasPrecision(18, 2);

        builder.Property(d => d.TotalReturns)
            .HasPrecision(18, 2);

        builder.Property(d => d.TotalVat)
            .HasPrecision(18, 2);

        builder.Property(d => d.CashTotal)
            .HasPrecision(18, 2);

        builder.Property(d => d.CardTotal)
            .HasPrecision(18, 2);

        builder.Property(d => d.OtherTotal)
            .HasPrecision(18, 2);

        builder.Property(d => d.TransactionCount);

        builder.Property(d => d.Status)
            .HasConversion<string>();

        builder.Property(d => d.ClosedBy);

        builder.Property(d => d.ClosedAt);

        builder.HasIndex(d => new { d.StoreId, d.ClosingDate })
            .IsUnique()
            .HasDatabaseName("IX_daily_closings_store_date");

        // Status filter for draft/finalized closings
        builder.HasIndex(d => d.Status)
            .HasDatabaseName("IX_daily_closings_status");
    }
}
