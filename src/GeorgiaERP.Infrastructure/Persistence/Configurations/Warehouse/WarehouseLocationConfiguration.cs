using GeorgiaERP.Domain.Warehouse;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Warehouse;

public class WarehouseLocationConfiguration : IEntityTypeConfiguration<WarehouseLocation>
{
    public void Configure(EntityTypeBuilder<WarehouseLocation> builder)
    {
        builder.ToTable("warehouse_locations");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(l => l.NameKa)
            .HasMaxLength(200);

        builder.Property(l => l.LocationType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(l => l.Notes)
            .HasMaxLength(500);

        builder.Property(l => l.IsActive)
            .HasDefaultValue(true);

        builder.HasOne(l => l.ParentLocation)
            .WithMany(l => l.ChildLocations)
            .HasForeignKey(l => l.ParentLocationId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => new { l.WarehouseId, l.Code })
            .IsUnique()
            .HasDatabaseName("IX_warehouse_locations_warehouse_code");

        builder.HasIndex(l => l.WarehouseId)
            .HasDatabaseName("IX_warehouse_locations_warehouse");

        builder.HasIndex(l => l.ParentLocationId)
            .HasDatabaseName("IX_warehouse_locations_parent")
            .HasFilter("\"ParentLocationId\" IS NOT NULL");
    }
}
