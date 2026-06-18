using GeorgiaERP.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Products;

public class ProductBundleConfiguration : IEntityTypeConfiguration<ProductBundle>
{
    public void Configure(EntityTypeBuilder<ProductBundle> builder)
    {
        builder.ToTable("product_bundles");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.BundleProductId);

        builder.Property(b => b.ComponentProductId);

        builder.Property(b => b.Quantity)
            .HasPrecision(18, 4);

        builder.HasIndex(b => new { b.BundleProductId, b.ComponentProductId })
            .IsUnique();

        builder.HasOne(b => b.BundleProduct)
            .WithMany()
            .HasForeignKey(b => b.BundleProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.ComponentProduct)
            .WithMany()
            .HasForeignKey(b => b.ComponentProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
