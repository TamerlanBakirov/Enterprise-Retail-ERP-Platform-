using GeorgiaERP.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Inventory;

public class StockLevelConfiguration : IEntityTypeConfiguration<StockLevel>
{
    public void Configure(EntityTypeBuilder<StockLevel> builder)
    {
        builder.ToTable("stock_levels");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.ProductId);

        builder.Property(s => s.VariantId);

        builder.Property(s => s.WarehouseId);

        builder.Property(s => s.LocationCode)
            .HasMaxLength(50);

        builder.Property(s => s.QuantityOnHand)
            .HasPrecision(18, 4)
            .HasDefaultValue(0m);

        builder.Property(s => s.QuantityReserved)
            .HasPrecision(18, 4)
            .HasDefaultValue(0m);

        builder.Property(s => s.QuantityInTransit)
            .HasPrecision(18, 4)
            .HasDefaultValue(0m);

        builder.Property(s => s.CostPrice)
            .HasPrecision(18, 2);

        builder.Property(s => s.LastCountDate);

        builder.Property(s => s.RowVersion)
            .IsConcurrencyToken();

        builder.Property(s => s.UpdatedAt);

        builder.HasIndex(s => new { s.ProductId, s.VariantId, s.WarehouseId })
            .IsUnique()
            .HasDatabaseName("IX_stock_levels_product_variant_warehouse");

        // FK index for warehouse stock queries
        builder.HasIndex(s => s.WarehouseId)
            .HasDatabaseName("IX_stock_levels_warehouse");

        // Product stock across all warehouses
        builder.HasIndex(s => s.ProductId)
            .HasDatabaseName("IX_stock_levels_product");

        // Composite for fast product+warehouse lookup (without variant)
        builder.HasIndex(s => new { s.ProductId, s.WarehouseId })
            .HasDatabaseName("IX_stock_levels_product_warehouse");
    }
}
