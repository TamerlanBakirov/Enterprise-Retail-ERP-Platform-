using GeorgiaERP.Domain.POS;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.POS;

public class PosTransactionLineConfiguration : IEntityTypeConfiguration<PosTransactionLine>
{
    public void Configure(EntityTypeBuilder<PosTransactionLine> builder)
    {
        builder.ToTable("pos_transaction_lines");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.TransactionId);

        builder.Property(l => l.LineNumber);

        builder.Property(l => l.ProductId);

        builder.Property(l => l.VariantId);

        builder.Property(l => l.Barcode)
            .HasMaxLength(50);

        builder.Property(l => l.ProductName)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(l => l.Quantity)
            .HasPrecision(18, 4);

        builder.Property(l => l.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(l => l.DiscountAmount)
            .HasPrecision(18, 2);

        builder.Property(l => l.VatAmount)
            .HasPrecision(18, 2);

        builder.Property(l => l.LineTotal)
            .HasPrecision(18, 2);

        builder.Property(l => l.CostPrice)
            .HasPrecision(18, 2);

        builder.Property(l => l.DiscountReason)
            .HasMaxLength(200);

        builder.Property(l => l.PromotionId);

        builder.HasOne(l => l.Transaction)
            .WithMany(t => t.Lines)
            .HasForeignKey(l => l.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
