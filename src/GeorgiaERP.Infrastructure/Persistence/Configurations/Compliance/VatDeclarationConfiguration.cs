using GeorgiaERP.Domain.Compliance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Compliance;

public class VatDeclarationConfiguration : IEntityTypeConfiguration<VatDeclaration>
{
    public void Configure(EntityTypeBuilder<VatDeclaration> builder)
    {
        builder.ToTable("vat_declarations");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.PeriodStart);

        builder.Property(v => v.PeriodEnd);

        builder.Property(v => v.TotalOutputVat)
            .HasPrecision(18, 2);

        builder.Property(v => v.TotalInputVat)
            .HasPrecision(18, 2);

        builder.Property(v => v.NetVat)
            .HasPrecision(18, 2);

        builder.Property(v => v.Status)
            .HasConversion<string>();

        builder.Property(v => v.SubmittedAt);

        builder.Property(v => v.RsGeReference)
            .HasMaxLength(100);

        builder.Property(v => v.CreatedAt);

        builder.HasIndex(v => new { v.PeriodStart, v.PeriodEnd })
            .IsUnique();
    }
}
