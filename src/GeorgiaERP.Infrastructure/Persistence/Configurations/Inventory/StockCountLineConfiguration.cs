using GeorgiaERP.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Inventory;

public class StockCountLineConfiguration : IEntityTypeConfiguration<StockCountLine>
{
    public void Configure(EntityTypeBuilder<StockCountLine> builder)
    {
        builder.ToTable("stock_count_lines");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.StockCountId);

        builder.Property(s => s.ProductId);

        builder.Property(s => s.VariantId);

        builder.Property(s => s.ExpectedQty)
            .HasPrecision(18, 4);

        builder.Property(s => s.CountedQty)
            .HasPrecision(18, 4);

        builder.Property(s => s.CountedBy);

        builder.Property(s => s.CountedAt);

        builder.HasOne(s => s.StockCount)
            .WithMany(sc => sc.Lines)
            .HasForeignKey(s => s.StockCountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
