using GeorgiaERP.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Finance;

public class ChartOfAccountConfiguration : IEntityTypeConfiguration<ChartOfAccount>
{
    public void Configure(EntityTypeBuilder<ChartOfAccount> builder)
    {
        builder.ToTable("chart_of_accounts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AccountCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(a => a.AccountCode)
            .IsUnique();

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.NameKa)
            .HasMaxLength(200);

        builder.Property(a => a.AccountType)
            .HasConversion<string>();

        builder.Property(a => a.ParentId);

        builder.Property(a => a.IsHeader)
            .HasDefaultValue(false);

        builder.Property(a => a.IsSystem)
            .HasDefaultValue(false);

        builder.Property(a => a.BalanceType)
            .HasConversion<string>();

        builder.Property(a => a.IsActive)
            .HasDefaultValue(true);

        builder.Property(a => a.CreatedAt);

        builder.HasOne(a => a.Parent)
            .WithMany(a => a.Children)
            .HasForeignKey(a => a.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
