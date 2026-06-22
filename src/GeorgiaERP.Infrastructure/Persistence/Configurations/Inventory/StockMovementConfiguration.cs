using GeorgiaERP.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Inventory;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movements");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.MovementType)
            .HasConversion<string>();

        builder.Property(s => s.ProductId);

        builder.Property(s => s.VariantId);

        builder.Property(s => s.WarehouseId);

        builder.Property(s => s.Quantity)
            .HasPrecision(18, 4);

        builder.Property(s => s.CostPrice)
            .HasPrecision(18, 2);

        builder.Property(s => s.ReferenceType)
            .HasMaxLength(50);

        builder.Property(s => s.ReferenceId);

        builder.Property(s => s.BatchNumber)
            .HasMaxLength(50);

        builder.Property(s => s.SerialNumber)
            .HasMaxLength(100);

        builder.Property(s => s.ExpiryDate);

        builder.Property(s => s.Notes)
            .HasMaxLength(1000);

        builder.Property(s => s.CreatedAt);

        builder.Property(s => s.CreatedBy);

        // Composite index for product+warehouse stock movement history
        builder.HasIndex(s => new { s.ProductId, s.WarehouseId })
            .HasDatabaseName("IX_stock_movements_product_warehouse");

        // Date range queries for stock movement reports
        builder.HasIndex(s => s.CreatedAt)
            .HasDatabaseName("IX_stock_movements_created_at");

        // Composite index for product movement history ordered by date
        builder.HasIndex(s => new { s.ProductId, s.CreatedAt })
            .HasDatabaseName("IX_stock_movements_product_date");

        // Reference lookups - find movements by source document
        builder.HasIndex(s => new { s.ReferenceType, s.ReferenceId })
            .HasDatabaseName("IX_stock_movements_reference");

        // Movement type filtering (SALE, RECEIPT, ADJUSTMENT, etc.)
        builder.HasIndex(s => s.MovementType)
            .HasDatabaseName("IX_stock_movements_type");

        // FK index for warehouse lookups
        builder.HasIndex(s => s.WarehouseId)
            .HasDatabaseName("IX_stock_movements_warehouse");
    }
}
