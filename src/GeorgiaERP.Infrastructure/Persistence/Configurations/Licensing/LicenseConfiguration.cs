using GeorgiaERP.Domain.Licensing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Licensing;

public class LicenseConfiguration : IEntityTypeConfiguration<License>
{
    public void Configure(EntityTypeBuilder<License> builder)
    {
        builder.ToTable("licenses");

        builder.Property(l => l.LicenseKey).IsRequired().HasMaxLength(100);
        builder.Property(l => l.CompanyName).IsRequired().HasMaxLength(200);
        builder.Property(l => l.ContactEmail).HasMaxLength(200);
        builder.Property(l => l.MachineId).IsRequired().HasMaxLength(64);
        builder.Property(l => l.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.Features).HasColumnType("jsonb");
        builder.Property(l => l.MaxUsers).HasDefaultValue(5);
        builder.Property(l => l.MaxStores).HasDefaultValue(1);

        builder.HasIndex(l => l.LicenseKey).IsUnique();
        builder.HasIndex(l => l.MachineId);
    }
}
