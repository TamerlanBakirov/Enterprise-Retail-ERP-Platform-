using GeorgiaERP.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Products;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Sku)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(p => p.Sku)
            .IsUnique();

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(p => p.NameKa)
            .HasMaxLength(300);

        builder.Property(p => p.Description)
            .HasMaxLength(2000);

        builder.Property(p => p.CategoryId);

        builder.Property(p => p.UnitOfMeasure)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(p => p.RsGeUnitId)
            .HasMaxLength(20);

        builder.Property(p => p.VatApplicable)
            .HasDefaultValue(true);

        builder.Property(p => p.ExciseCode)
            .HasMaxLength(20);

        builder.Property(p => p.WeightKg)
            .HasPrecision(18, 4);

        builder.Property(p => p.VolumeL)
            .HasPrecision(18, 4);

        builder.Property(p => p.WidthCm)
            .HasPrecision(18, 4);

        builder.Property(p => p.HeightCm)
            .HasPrecision(18, 4);

        builder.Property(p => p.DepthCm)
            .HasPrecision(18, 4);

        builder.Property(p => p.MinStockLevel)
            .HasPrecision(18, 4);

        builder.Property(p => p.MaxStockLevel)
            .HasPrecision(18, 4);

        builder.Property(p => p.ReorderPoint)
            .HasPrecision(18, 4);

        builder.Property(p => p.ReorderQty)
            .HasPrecision(18, 4);

        builder.Property(p => p.IsSerialized)
            .HasDefaultValue(false);

        builder.Property(p => p.IsBatchTracked)
            .HasDefaultValue(false);

        builder.Property(p => p.HasExpiry)
            .HasDefaultValue(false);

        builder.Property(p => p.IsActive)
            .HasDefaultValue(true);

        builder.Property(p => p.ImageUrl)
            .HasMaxLength(1000);

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Variants)
            .WithOne(v => v.Product)
            .HasForeignKey(v => v.ProductId);

        builder.HasMany(p => p.Barcodes)
            .WithOne(b => b.Product)
            .HasForeignKey(b => b.ProductId);

        // Category FK index with active filter for product listings
        builder.HasIndex(p => p.CategoryId)
            .HasDatabaseName("IX_products_category");

        // Active product filtering (most queries filter by IsActive)
        builder.HasIndex(p => p.IsActive)
            .HasDatabaseName("IX_products_active");

        // Name search support
        builder.HasIndex(p => p.Name)
            .HasDatabaseName("IX_products_name");
    }
}
