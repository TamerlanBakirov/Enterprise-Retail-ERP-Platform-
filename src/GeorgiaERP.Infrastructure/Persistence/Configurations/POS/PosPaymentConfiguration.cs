using GeorgiaERP.Domain.POS;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.POS;

public class PosPaymentConfiguration : IEntityTypeConfiguration<PosPayment>
{
    public void Configure(EntityTypeBuilder<PosPayment> builder)
    {
        builder.ToTable("pos_payments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.TransactionId);

        builder.Property(p => p.PaymentMethod)
            .HasConversion<string>();

        builder.Property(p => p.Amount)
            .HasPrecision(18, 2);

        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("GEL");

        builder.Property(p => p.Reference)
            .HasMaxLength(100);

        builder.Property(p => p.TerminalRef)
            .HasMaxLength(100);

        builder.Property(p => p.ChangeAmount)
            .HasPrecision(18, 2);

        builder.Property(p => p.CreatedAt);

        builder.HasOne(p => p.Transaction)
            .WithMany(t => t.Payments)
            .HasForeignKey(p => p.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
