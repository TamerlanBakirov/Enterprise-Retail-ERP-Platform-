using GeorgiaERP.Domain.Procurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Procurement;

public class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
    {
        builder.ToTable("purchase_order_lines");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.PurchaseOrderId);

        builder.Property(p => p.LineNumber);

        builder.Property(p => p.ProductId);

        builder.Property(p => p.VariantId);

        builder.Property(p => p.OrderedQty)
            .HasPrecision(18, 4);

        builder.Property(p => p.ReceivedQty)
            .HasPrecision(18, 4);

        builder.Property(p => p.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(p => p.VatAmount)
            .HasPrecision(18, 2);

        builder.Property(p => p.LineTotal)
            .HasPrecision(18, 2);

        builder.HasOne(p => p.PurchaseOrder)
            .WithMany(po => po.Lines)
            .HasForeignKey(p => p.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // FK index for purchase order line lookups
        builder.HasIndex(p => p.PurchaseOrderId)
            .HasDatabaseName("IX_purchase_order_lines_purchase_order");

        // Product procurement history
        builder.HasIndex(p => p.ProductId)
            .HasDatabaseName("IX_purchase_order_lines_product");
    }
}
