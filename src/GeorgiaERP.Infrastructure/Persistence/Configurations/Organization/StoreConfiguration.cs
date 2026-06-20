using GeorgiaERP.Domain.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Organization;

public class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.ToTable("stores");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(s => s.Code)
            .IsUnique();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.NameKa)
            .HasMaxLength(200);

        builder.Property(s => s.StoreType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(s => s.Address)
            .HasMaxLength(500);

        builder.Property(s => s.City)
            .HasMaxLength(100);

        builder.Property(s => s.Region)
            .HasMaxLength(100);

        builder.Property(s => s.Phone)
            .HasMaxLength(20);

        builder.Property(s => s.ManagerUserId);

        builder.Property(s => s.Latitude);

        builder.Property(s => s.Longitude);

        builder.Property(s => s.Timezone)
            .HasMaxLength(50)
            .HasDefaultValue("Asia/Tbilisi");

        builder.Property(s => s.IsActive)
            .HasDefaultValue(true);

        builder.Property(s => s.Settings)
            .HasColumnType("jsonb");

        builder.Property(s => s.CreatedAt);

        builder.Property(s => s.UpdatedAt);
    }
}
