using GeorgiaERP.Domain.Procurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Procurement;

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("purchase_orders");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.PoNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(p => p.PoNumber)
            .IsUnique();

        builder.Property(p => p.SupplierId);

        builder.Property(p => p.WarehouseId);

        builder.Property(p => p.Status)
            .HasConversion<string>();

        builder.Property(p => p.OrderDate);

        builder.Property(p => p.ExpectedDate);

        builder.Property(p => p.Subtotal)
            .HasPrecision(18, 2);

        builder.Property(p => p.VatTotal)
            .HasPrecision(18, 2);

        builder.Property(p => p.Total)
            .HasPrecision(18, 2);

        builder.Property(p => p.Notes)
            .HasMaxLength(1000);

        builder.Property(p => p.CreatedBy);

        builder.Property(p => p.ApprovedBy);

        builder.Property(p => p.ApprovedAt);

        builder.Property(p => p.CreatedAt);

        builder.Property(p => p.UpdatedAt);

        builder.HasOne(p => p.Supplier)
            .WithMany(s => s.PurchaseOrders)
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Lines)
            .WithOne(l => l.PurchaseOrder)
            .HasForeignKey(l => l.PurchaseOrderId);
    }
}
