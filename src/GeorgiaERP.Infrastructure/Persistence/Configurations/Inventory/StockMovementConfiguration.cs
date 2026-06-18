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

        builder.HasIndex(s => new { s.ProductId, s.WarehouseId });
    }
}
