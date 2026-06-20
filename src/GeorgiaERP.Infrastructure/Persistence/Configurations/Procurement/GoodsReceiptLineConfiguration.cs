using GeorgiaERP.Domain.Procurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Procurement;

public class GoodsReceiptLineConfiguration : IEntityTypeConfiguration<GoodsReceiptLine>
{
    public void Configure(EntityTypeBuilder<GoodsReceiptLine> builder)
    {
        builder.ToTable("goods_receipt_lines");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.GrnId);

        builder.Property(g => g.PoLineId);

        builder.Property(g => g.ProductId);

        builder.Property(g => g.VariantId);

        builder.Property(g => g.ReceivedQty)
            .HasPrecision(18, 4);

        builder.Property(g => g.AcceptedQty)
            .HasPrecision(18, 4);

        builder.Property(g => g.RejectedQty)
            .HasPrecision(18, 4);

        builder.Property(g => g.BatchNumber)
            .HasMaxLength(50);

        builder.Property(g => g.ExpiryDate);

        builder.Property(g => g.UnitCost)
            .HasPrecision(18, 2);

        builder.HasOne(g => g.GoodsReceiptNote)
            .WithMany(grn => grn.Lines)
            .HasForeignKey(g => g.GrnId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(g => g.PurchaseOrderLine)
            .WithMany()
            .HasForeignKey(g => g.PoLineId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
