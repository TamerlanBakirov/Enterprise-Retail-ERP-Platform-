using GeorgiaERP.Domain.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Pricing;

public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.ToTable("promotions");

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

        builder.Property(p => p.PromotionType)
            .HasConversion<string>();

        builder.Property(p => p.DiscountValue)
            .HasPrecision(18, 2);

        builder.Property(p => p.Conditions)
            .HasColumnType("jsonb");

        builder.Property(p => p.StoreIds)
            .HasColumnType("jsonb");

        builder.Property(p => p.ValidFrom);

        builder.Property(p => p.ValidTo);

        builder.Property(p => p.IsActive)
            .HasDefaultValue(true);

        builder.Property(p => p.MaxUses);

        builder.Property(p => p.CurrentUses)
            .HasDefaultValue(0);

        builder.Property(p => p.CreatedAt);

        // Active promotions with validity window - POS promotion lookup
        builder.HasIndex(p => new { p.IsActive, p.ValidFrom, p.ValidTo })
            .HasDatabaseName("IX_promotions_active_validity");
    }
}
