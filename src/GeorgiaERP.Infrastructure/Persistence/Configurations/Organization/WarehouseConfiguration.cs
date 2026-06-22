using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseEntity = GeorgiaERP.Domain.Organization.Warehouse;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Organization;

public class WarehouseConfiguration : IEntityTypeConfiguration<WarehouseEntity>
{
    public void Configure(EntityTypeBuilder<WarehouseEntity> builder)
    {
        builder.ToTable("warehouses");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(w => w.Code)
            .IsUnique();

        builder.Property(w => w.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(w => w.NameKa)
            .HasMaxLength(200);

        builder.Property(w => w.WarehouseType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(w => w.Address)
            .HasMaxLength(500);

        builder.Property(w => w.City)
            .HasMaxLength(100);

        builder.Property(w => w.Region)
            .HasMaxLength(100);

        builder.Property(w => w.LinkedStoreId);

        builder.Property(w => w.IsActive)
            .HasDefaultValue(true);

        builder.Property(w => w.Settings)
            .HasColumnType("jsonb");

        builder.Property(w => w.CreatedAt);

        builder.Property(w => w.UpdatedAt);

        builder.HasOne(w => w.LinkedStore)
            .WithMany()
            .HasForeignKey(w => w.LinkedStoreId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        // Linked store lookup (find warehouse for a store)
        builder.HasIndex(w => w.LinkedStoreId)
            .HasDatabaseName("IX_warehouses_linked_store")
            .HasFilter("\"LinkedStoreId\" IS NOT NULL");

        // Active warehouse filtering
        builder.HasIndex(w => w.IsActive)
            .HasDatabaseName("IX_warehouses_active");
    }
}
