using GeorgiaERP.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Inventory;

public class StockCountConfiguration : IEntityTypeConfiguration<StockCount>
{
    public void Configure(EntityTypeBuilder<StockCount> builder)
    {
        builder.ToTable("stock_counts");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.WarehouseId);

        builder.Property(s => s.CountType)
            .HasConversion<string>();

        builder.Property(s => s.Status)
            .HasConversion<string>();

        builder.Property(s => s.StartedAt);

        builder.Property(s => s.CompletedAt);

        builder.Property(s => s.CreatedBy);

        builder.Property(s => s.ApprovedBy);

        builder.Property(s => s.CreatedAt);

        builder.HasMany(s => s.Lines)
            .WithOne(l => l.StockCount)
            .HasForeignKey(l => l.StockCountId);

        // FK index for warehouse stock count lookups
        builder.HasIndex(s => s.WarehouseId)
            .HasDatabaseName("IX_stock_counts_warehouse");

        // Status filter for finding active/in-progress counts
        builder.HasIndex(s => s.Status)
            .HasDatabaseName("IX_stock_counts_status");
    }
}
