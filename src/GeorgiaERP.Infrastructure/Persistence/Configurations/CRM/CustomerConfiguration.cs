using GeorgiaERP.Domain.CRM;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.CRM;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CustomerNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(c => c.CustomerNumber)
            .IsUnique();

        builder.Property(c => c.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.FirstNameKa)
            .HasMaxLength(100);

        builder.Property(c => c.LastNameKa)
            .HasMaxLength(100);

        builder.Property(c => c.CompanyName)
            .HasMaxLength(200);

        builder.Property(c => c.Tin)
            .HasMaxLength(20);

        builder.Property(c => c.Phone)
            .HasMaxLength(20);

        builder.Property(c => c.Email)
            .HasMaxLength(255);

        builder.Property(c => c.DateOfBirth);

        builder.Property(c => c.Gender)
            .HasMaxLength(10);

        builder.Property(c => c.LoyaltyCardNumber)
            .HasMaxLength(50);

        builder.HasIndex(c => c.LoyaltyCardNumber)
            .IsUnique()
            .HasFilter("\"LoyaltyCardNumber\" IS NOT NULL");

        builder.Property(c => c.LoyaltyTier)
            .HasMaxLength(20);

        builder.Property(c => c.LoyaltyPoints)
            .HasDefaultValue(0);

        builder.Property(c => c.TotalPurchases)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(c => c.TotalVisits)
            .HasDefaultValue(0);

        builder.Property(c => c.LastVisitAt);

        builder.Property(c => c.IsActive)
            .HasDefaultValue(true);

        builder.Property(c => c.ConsentSms)
            .HasDefaultValue(false);

        builder.Property(c => c.ConsentEmail)
            .HasDefaultValue(false);

        builder.Property(c => c.CreatedAt);

        builder.Property(c => c.UpdatedAt);

        builder.HasMany(c => c.LoyaltyTransactions)
            .WithOne(lt => lt.Customer)
            .HasForeignKey(lt => lt.CustomerId);
    }
}
