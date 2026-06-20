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
    }
}
