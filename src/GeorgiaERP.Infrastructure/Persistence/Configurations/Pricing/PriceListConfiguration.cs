using GeorgiaERP.Domain.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Pricing;

public class PriceListConfiguration : IEntityTypeConfiguration<PriceList>
{
    public void Configure(EntityTypeBuilder<PriceList> builder)
    {
        builder.ToTable("price_lists");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(p => p.Code)
            .IsUnique();

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.NameKa)
            .HasMaxLength(200);

        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("GEL");

        builder.Property(p => p.PriceType)
            .HasConversion<string>();

        builder.Property(p => p.StoreId);

        builder.Property(p => p.ValidFrom);

        builder.Property(p => p.ValidTo);

        builder.Property(p => p.IsActive)
            .HasDefaultValue(true);

        builder.Property(p => p.Priority)
            .HasDefaultValue(0);

        builder.Property(p => p.CreatedAt);

        builder.Property(p => p.UpdatedAt);

        builder.HasMany(p => p.Items)
            .WithOne(i => i.PriceList)
            .HasForeignKey(i => i.PriceListId);

        // Store-specific price list lookups
        builder.HasIndex(p => p.StoreId)
            .HasDatabaseName("IX_price_lists_store")
            .HasFilter("\"StoreId\" IS NOT NULL");

        // Active price lists with validity period
        builder.HasIndex(p => new { p.IsActive, p.ValidFrom, p.ValidTo })
            .HasDatabaseName("IX_price_lists_active_validity");

        // Price type filtering (RETAIL, WHOLESALE, etc.)
        builder.HasIndex(p => p.PriceType)
            .HasDatabaseName("IX_price_lists_type");
    }
}
