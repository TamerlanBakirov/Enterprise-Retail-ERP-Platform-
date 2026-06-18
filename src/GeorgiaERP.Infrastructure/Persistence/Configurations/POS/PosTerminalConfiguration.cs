using GeorgiaERP.Domain.POS;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.POS;

public class PosTerminalConfiguration : IEntityTypeConfiguration<PosTerminal>
{
    public void Configure(EntityTypeBuilder<PosTerminal> builder)
    {
        builder.ToTable("pos_terminals");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(t => t.Code)
            .IsUnique();

        builder.Property(t => t.StoreId);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.TerminalType)
            .HasConversion<string>();

        builder.Property(t => t.IsActive)
            .HasDefaultValue(true);

        builder.Property(t => t.Settings)
            .HasColumnType("jsonb");

        builder.Property(t => t.CreatedAt);

        builder.HasMany(t => t.Sessions)
            .WithOne(s => s.Terminal)
            .HasForeignKey(s => s.TerminalId);
    }
}
