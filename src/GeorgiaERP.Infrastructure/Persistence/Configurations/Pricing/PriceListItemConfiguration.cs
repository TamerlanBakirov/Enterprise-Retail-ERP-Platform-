using GeorgiaERP.Domain.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Pricing;

public class PriceListItemConfiguration : IEntityTypeConfiguration<PriceListItem>
{
    public void Configure(EntityTypeBuilder<PriceListItem> builder)
    {
        builder.ToTable("price_list_items");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.PriceListId);

        builder.Property(p => p.ProductId);

        builder.Property(p => p.VariantId);

        builder.Property(p => p.Price)
            .HasPrecision(18, 2);

        builder.Property(p => p.MinQty)
            .HasPrecision(18, 4);

        builder.HasOne(p => p.PriceList)
            .WithMany(pl => pl.Items)
            .HasForeignKey(p => p.PriceListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.PriceListId, p.ProductId, p.VariantId })
            .IsUnique()
            .HasDatabaseName("IX_price_list_items_list_product_variant");

        // Product price lookup across all price lists
        builder.HasIndex(p => p.ProductId)
            .HasDatabaseName("IX_price_list_items_product");
    }
}
