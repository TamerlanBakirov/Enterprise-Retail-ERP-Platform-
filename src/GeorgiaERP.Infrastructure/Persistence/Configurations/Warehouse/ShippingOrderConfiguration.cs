using GeorgiaERP.Domain.Warehouse;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Warehouse;

public class ShippingOrderConfiguration : IEntityTypeConfiguration<ShippingOrder>
{
    public void Configure(EntityTypeBuilder<ShippingOrder> builder)
    {
        builder.ToTable("shipping_orders");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.ShippingNumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.HasIndex(s => s.ShippingNumber)
            .IsUnique();

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(s => s.OrderType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(s => s.ShippingAddress)
            .HasMaxLength(500);

        builder.Property(s => s.TrackingNumber)
            .HasMaxLength(100);

        builder.Property(s => s.Carrier)
            .HasMaxLength(100);

        builder.Property(s => s.Notes)
            .HasMaxLength(1000);

        builder.HasMany(s => s.Lines)
            .WithOne(l => l.ShippingOrder)
            .HasForeignKey(l => l.ShippingOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.WarehouseId)
            .HasDatabaseName("IX_shipping_orders_warehouse");

        builder.HasIndex(s => s.Status)
            .HasDatabaseName("IX_shipping_orders_status");

        builder.HasIndex(s => s.CustomerId)
            .HasDatabaseName("IX_shipping_orders_customer")
            .HasFilter("\"CustomerId\" IS NOT NULL");

        builder.HasIndex(s => s.SourceOrderId)
            .HasDatabaseName("IX_shipping_orders_source")
            .HasFilter("\"SourceOrderId\" IS NOT NULL");

        builder.HasIndex(s => s.RsGeWaybillId)
            .HasDatabaseName("IX_shipping_orders_waybill")
            .HasFilter("\"RsGeWaybillId\" IS NOT NULL");
    }
}

public class ShippingOrderLineConfiguration : IEntityTypeConfiguration<ShippingOrderLine>
{
    public void Configure(EntityTypeBuilder<ShippingOrderLine> builder)
    {
        builder.ToTable("shipping_order_lines");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.OrderedQty)
            .HasPrecision(18, 4);

        builder.Property(l => l.PickedQty)
            .HasPrecision(18, 4);

        builder.Property(l => l.PackedQty)
            .HasPrecision(18, 4);

        builder.Property(l => l.ShippedQty)
            .HasPrecision(18, 4);

        builder.Property(l => l.BatchNumber)
            .HasMaxLength(100);

        builder.Property(l => l.SerialNumber)
            .HasMaxLength(100);

        builder.Property(l => l.Notes)
            .HasMaxLength(500);

        builder.HasIndex(l => l.ProductId)
            .HasDatabaseName("IX_shipping_order_lines_product");
    }
}
