using GeorgiaERP.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Inventory;

public class TransferOrderConfiguration : IEntityTypeConfiguration<TransferOrder>
{
    public void Configure(EntityTypeBuilder<TransferOrder> builder)
    {
        builder.ToTable("transfer_orders");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TransferNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(t => t.TransferNumber)
            .IsUnique();

        builder.Property(t => t.SourceWarehouseId);

        builder.Property(t => t.DestWarehouseId);

        builder.Property(t => t.Status)
            .HasConversion<string>();

        builder.Property(t => t.RsGeWaybillId);

        builder.Property(t => t.RequestedBy);

        builder.Property(t => t.ApprovedBy);

        builder.Property(t => t.ShippedAt);

        builder.Property(t => t.ReceivedAt);

        builder.Property(t => t.Notes)
            .HasMaxLength(1000);

        builder.Property(t => t.CreatedAt);

        builder.Property(t => t.UpdatedAt);

        builder.HasMany(t => t.Lines)
            .WithOne(l => l.TransferOrder)
            .HasForeignKey(l => l.TransferOrderId);

        // FK index for source warehouse transfer lookups
        builder.HasIndex(t => t.SourceWarehouseId)
            .HasDatabaseName("IX_transfer_orders_source_warehouse");

        // FK index for destination warehouse transfer lookups
        builder.HasIndex(t => t.DestWarehouseId)
            .HasDatabaseName("IX_transfer_orders_dest_warehouse");

        // Status filter for pending/in-transit orders
        builder.HasIndex(t => t.Status)
            .HasDatabaseName("IX_transfer_orders_status");

        // Date ordering for transfer history
        builder.HasIndex(t => t.CreatedAt)
            .HasDatabaseName("IX_transfer_orders_created_at");
    }
}
