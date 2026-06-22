using GeorgiaERP.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Finance;

public class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.ToTable("bank_accounts");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.AccountName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.BankName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.AccountNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(b => b.AccountNumber)
            .IsUnique();

        builder.Property(b => b.Iban)
            .HasMaxLength(34);

        builder.Property(b => b.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("GEL");

        builder.Property(b => b.GlAccountId);

        builder.Property(b => b.CurrentBalance)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(b => b.IsActive)
            .HasDefaultValue(true);

        builder.Property(b => b.CreatedAt);

        builder.HasOne(b => b.GlAccount)
            .WithMany()
            .HasForeignKey(b => b.GlAccountId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // FK index for GL account lookups
        builder.HasIndex(b => b.GlAccountId)
            .HasDatabaseName("IX_bank_accounts_gl_account");

        // IBAN lookup
        builder.HasIndex(b => b.Iban)
            .HasDatabaseName("IX_bank_accounts_iban")
            .HasFilter("\"Iban\" IS NOT NULL");
    }
}
