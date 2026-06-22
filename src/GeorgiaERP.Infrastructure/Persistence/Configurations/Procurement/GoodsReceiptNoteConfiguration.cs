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

        // FK index for purchase order receipt lookups
        builder.HasIndex(g => g.PurchaseOrderId)
            .HasDatabaseName("IX_goods_receipt_notes_purchase_order");

        // FK index for supplier receipt lookups
        builder.HasIndex(g => g.SupplierId)
            .HasDatabaseName("IX_goods_receipt_notes_supplier");

        // FK index for warehouse receipt lookups
        builder.HasIndex(g => g.WarehouseId)
            .HasDatabaseName("IX_goods_receipt_notes_warehouse");

        // Status filter for pending/completed receipts
        builder.HasIndex(g => g.Status)
            .HasDatabaseName("IX_goods_receipt_notes_status");

        // Receipt date for reporting
        builder.HasIndex(g => g.ReceiptDate)
            .HasDatabaseName("IX_goods_receipt_notes_receipt_date");
    }
}
