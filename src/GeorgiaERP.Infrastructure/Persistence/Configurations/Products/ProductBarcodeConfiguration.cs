using GeorgiaERP.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Products;

public class ProductBarcodeConfiguration : IEntityTypeConfiguration<ProductBarcode>
{
    public void Configure(EntityTypeBuilder<ProductBarcode> builder)
    {
        builder.ToTable("product_barcodes");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.ProductId);

        builder.Property(b => b.VariantId);

        builder.Property(b => b.Barcode)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(b => b.Barcode)
            .IsUnique();

        builder.Property(b => b.BarcodeType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(b => b.IsPrimary)
            .HasDefaultValue(false);

        builder.HasOne(b => b.Product)
            .WithMany(p => p.Barcodes)
            .HasForeignKey(b => b.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.Variant)
            .WithMany()
            .HasForeignKey(b => b.VariantId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        // FK index for product barcode lookups
        builder.HasIndex(b => b.ProductId)
            .HasDatabaseName("IX_product_barcodes_product");

        // Primary-barcode lookup on the POS hot path (resolve a product's barcode).
        builder.HasIndex(b => new { b.ProductId, b.IsPrimary })
            .HasDatabaseName("IX_product_barcodes_product_primary");
    }
}
