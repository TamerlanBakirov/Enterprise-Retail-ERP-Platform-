using GeorgiaERP.Domain.Warehouse;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Warehouse;

public class ReceivingOrderConfiguration : IEntityTypeConfiguration<ReceivingOrder>
{
    public void Configure(EntityTypeBuilder<ReceivingOrder> builder)
    {
        builder.ToTable("receiving_orders");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReceivingNumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.HasIndex(r => r.ReceivingNumber)
            .IsUnique();

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(r => r.Source)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(r => r.Notes)
            .HasMaxLength(1000);

        builder.HasMany(r => r.Lines)
            .WithOne(l => l.ReceivingOrder)
            .HasForeignKey(l => l.ReceivingOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.WarehouseId)
            .HasDatabaseName("IX_receiving_orders_warehouse");

        builder.HasIndex(r => r.Status)
            .HasDatabaseName("IX_receiving_orders_status");

        builder.HasIndex(r => r.SourceOrderId)
            .HasDatabaseName("IX_receiving_orders_source")
            .HasFilter("\"SourceOrderId\" IS NOT NULL");
    }
}

public class ReceivingOrderLineConfiguration : IEntityTypeConfiguration<ReceivingOrderLine>
{
    public void Configure(EntityTypeBuilder<ReceivingOrderLine> builder)
    {
        builder.ToTable("receiving_order_lines");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.ExpectedQty)
            .HasPrecision(18, 4);

        builder.Property(l => l.ReceivedQty)
            .HasPrecision(18, 4);

        builder.Property(l => l.DamagedQty)
            .HasPrecision(18, 4);

        builder.Property(l => l.BatchNumber)
            .HasMaxLength(100);

        builder.Property(l => l.SerialNumber)
            .HasMaxLength(100);

        builder.Property(l => l.Notes)
            .HasMaxLength(500);

        builder.HasIndex(l => l.ProductId)
            .HasDatabaseName("IX_receiving_order_lines_product");
    }
}
