using GeorgiaERP.Domain.Procurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Procurement;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("suppliers");

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

        builder.Property(s => s.Tin)
            .HasMaxLength(20);

        builder.Property(s => s.IsVatPayer)
            .HasDefaultValue(false);

        builder.Property(s => s.ContactPerson)
            .HasMaxLength(200);

        builder.Property(s => s.Phone)
            .HasMaxLength(20);

        builder.Property(s => s.Email)
            .HasMaxLength(255);

        builder.Property(s => s.Address)
            .HasMaxLength(500);

        builder.Property(s => s.PaymentTerms)
            .HasMaxLength(100);

        builder.Property(s => s.CreditLimit)
            .HasPrecision(18, 2);

        builder.Property(s => s.Rating);

        builder.Property(s => s.IsActive)
            .HasDefaultValue(true);

        builder.Property(s => s.Settings)
            .HasColumnType("jsonb");

        builder.Property(s => s.CreatedAt);

        builder.Property(s => s.UpdatedAt);

        builder.HasMany(s => s.PurchaseOrders)
            .WithOne(po => po.Supplier)
            .HasForeignKey(po => po.SupplierId);

        // TIN lookup for RS.GE supplier matching
        builder.HasIndex(s => s.Tin)
            .HasDatabaseName("IX_suppliers_tin")
            .HasFilter("\"Tin\" IS NOT NULL");

        // Active supplier filtering
        builder.HasIndex(s => s.IsActive)
            .HasDatabaseName("IX_suppliers_active");
    }
}
