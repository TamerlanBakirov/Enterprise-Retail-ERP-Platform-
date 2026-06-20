using GeorgiaERP.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Inventory;

public class TransferOrderLineConfiguration : IEntityTypeConfiguration<TransferOrderLine>
{
    public void Configure(EntityTypeBuilder<TransferOrderLine> builder)
    {
        builder.ToTable("transfer_order_lines");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TransferOrderId);

        builder.Property(t => t.ProductId);

        builder.Property(t => t.VariantId);

        builder.Property(t => t.RequestedQty)
            .HasPrecision(18, 4);

        builder.Property(t => t.ShippedQty)
            .HasPrecision(18, 4);

        builder.Property(t => t.ReceivedQty)
            .HasPrecision(18, 4);

        builder.Property(t => t.BatchNumber)
            .HasMaxLength(50);

        builder.Property(t => t.SerialNumber)
            .HasMaxLength(100);

        builder.HasOne(t => t.TransferOrder)
            .WithMany(to => to.Lines)
            .HasForeignKey(t => t.TransferOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
