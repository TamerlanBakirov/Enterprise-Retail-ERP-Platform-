using GeorgiaERP.Domain.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Organization;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("companies");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(c => c.Code)
            .IsUnique();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.NameKa)
            .HasMaxLength(200);

        builder.Property(c => c.Tin)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(c => c.Tin)
            .IsUnique();

        builder.Property(c => c.IsVatPayer)
            .HasDefaultValue(false);

        builder.Property(c => c.VatRegistrationDate);

        builder.Property(c => c.LegalAddress)
            .HasMaxLength(500);

        builder.Property(c => c.ActualAddress)
            .HasMaxLength(500);

        builder.Property(c => c.Phone)
            .HasMaxLength(20);

        builder.Property(c => c.Email)
            .HasMaxLength(255);

        builder.Property(c => c.Settings)
            .HasColumnType("jsonb");

        builder.Property(c => c.IsActive)
            .HasDefaultValue(true);

        builder.Property(c => c.CreatedAt);

        builder.Property(c => c.UpdatedAt);
    }
}
