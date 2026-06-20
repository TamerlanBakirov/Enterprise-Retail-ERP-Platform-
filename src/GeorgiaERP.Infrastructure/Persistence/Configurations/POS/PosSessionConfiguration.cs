using GeorgiaERP.Domain.POS;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.POS;

public class PosSessionConfiguration : IEntityTypeConfiguration<PosSession>
{
    public void Configure(EntityTypeBuilder<PosSession> builder)
    {
        builder.ToTable("pos_sessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.TerminalId);

        builder.Property(s => s.CashierId);

        builder.Property(s => s.OpenedAt);

        builder.Property(s => s.ClosedAt);

        builder.Property(s => s.OpeningBalance)
            .HasPrecision(18, 2);

        builder.Property(s => s.ClosingBalance)
            .HasPrecision(18, 2);

        builder.Property(s => s.ExpectedBalance)
            .HasPrecision(18, 2);

        builder.Property(s => s.CashDifference)
            .HasPrecision(18, 2);

        builder.Property(s => s.Status)
            .HasConversion<string>();

        builder.Property(s => s.Notes)
            .HasMaxLength(500);

        builder.HasOne(s => s.Terminal)
            .WithMany(t => t.Sessions)
            .HasForeignKey(s => s.TerminalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Transactions)
            .WithOne(t => t.Session)
            .HasForeignKey(t => t.SessionId);
    }
}
