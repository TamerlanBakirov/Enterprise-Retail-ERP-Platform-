using GeorgiaERP.Domain.Compliance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Compliance;

public class RsGeWaybillConfiguration : IEntityTypeConfiguration<RsGeWaybill>
{
    public void Configure(EntityTypeBuilder<RsGeWaybill> builder)
    {
        builder.ToTable("rsge_waybills");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.FiscalDocumentId);

        builder.HasIndex(w => w.FiscalDocumentId)
            .IsUnique();

        builder.Property(w => w.WaybillNumber)
            .HasMaxLength(50);

        builder.Property(w => w.WaybillType)
            .HasMaxLength(20);

        builder.Property(w => w.SellerTin)
            .HasMaxLength(20);

        builder.Property(w => w.BuyerTin)
            .HasMaxLength(20);

        builder.Property(w => w.TransporterTin)
            .HasMaxLength(20);

        builder.Property(w => w.DriverTin)
            .HasMaxLength(20);

        builder.Property(w => w.SellerName)
            .HasMaxLength(200);

        builder.Property(w => w.BuyerName)
            .HasMaxLength(200);

        builder.Property(w => w.TransportType)
            .HasMaxLength(50);

        builder.Property(w => w.VehicleNumber)
            .HasMaxLength(20);

        builder.Property(w => w.StartAddress)
            .HasMaxLength(500);

        builder.Property(w => w.EndAddress)
            .HasMaxLength(500);

        builder.Property(w => w.GoodsData)
            .HasColumnType("jsonb");

        builder.Property(w => w.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(w => w.ActivateDate);

        builder.Property(w => w.DeliveryDate);

        builder.Property(w => w.Status)
            .HasConversion<string>();

        builder.Property(w => w.CreatedAt);

        builder.Property(w => w.UpdatedAt);

        builder.HasOne(w => w.FiscalDocument)
            .WithMany()
            .HasForeignKey(w => w.FiscalDocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Waybill number lookup
        builder.HasIndex(w => w.WaybillNumber)
            .HasDatabaseName("IX_rsge_waybills_number")
            .HasFilter("\"WaybillNumber\" IS NOT NULL");

        // Status filter for active/pending waybills
        builder.HasIndex(w => w.Status)
            .HasDatabaseName("IX_rsge_waybills_status");

        // Seller TIN lookup for compliance
        builder.HasIndex(w => w.SellerTin)
            .HasDatabaseName("IX_rsge_waybills_seller_tin");

        // Buyer TIN lookup for compliance
        builder.HasIndex(w => w.BuyerTin)
            .HasDatabaseName("IX_rsge_waybills_buyer_tin")
            .HasFilter("\"BuyerTin\" IS NOT NULL");
    }
}
