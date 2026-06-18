using GeorgiaERP.Domain.Procurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Procurement;

public class GoodsReceiptNoteConfiguration : IEntityTypeConfiguration<GoodsReceiptNote>
{
    public void Configure(EntityTypeBuilder<GoodsReceiptNote> builder)
    {
        builder.ToTable("goods_receipt_notes");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.GrnNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(g => g.GrnNumber)
            .IsUnique();

        builder.Property(g => g.PurchaseOrderId);

        builder.Property(g => g.WarehouseId);

        builder.Property(g => g.SupplierId);

        builder.Property(g => g.RsGeWaybillId);

        builder.Property(g => g.ReceiptDate);

        builder.Property(g => g.Status)
            .HasConversion<string>();

        builder.Property(g => g.Notes)
            .HasMaxLength(1000);

        builder.Property(g => g.ReceivedBy);

        builder.Property(g => g.CreatedAt);

        builder.HasOne(g => g.PurchaseOrder)
            .WithMany()
            .HasForeignKey(g => g.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(g => g.Supplier)
            .WithMany()
            .HasForeignKey(g => g.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(g => g.Lines)
            .WithOne(l => l.GoodsReceiptNote)
            .HasForeignKey(l => l.GrnId);
    }
}
