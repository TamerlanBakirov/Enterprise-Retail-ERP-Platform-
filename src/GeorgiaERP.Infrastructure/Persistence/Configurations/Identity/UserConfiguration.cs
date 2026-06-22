using GeorgiaERP.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Identity;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.FirstNameKa)
            .HasMaxLength(100);

        builder.Property(u => u.LastNameKa)
            .HasMaxLength(100);

        builder.Property(u => u.Phone)
            .HasMaxLength(20);

        builder.Property(u => u.DefaultStoreId);

        builder.Property(u => u.DefaultLanguage)
            .IsRequired()
            .HasMaxLength(5)
            .HasDefaultValue("ka");

        builder.Property(u => u.Is2FaEnabled)
            .HasDefaultValue(false);

        builder.Property(u => u.TotpSecret)
            .HasMaxLength(500);

        builder.Property(u => u.FailedLoginCount)
            .HasDefaultValue(0);

        builder.Property(u => u.LockedUntil);

        builder.Property(u => u.LastLoginAt);

        builder.Property(u => u.ResetToken)
            .HasMaxLength(200);

        builder.Property(u => u.ResetTokenExpiry);

        builder.Property(u => u.IsActive)
            .HasDefaultValue(true);

        builder.Property(u => u.CreatedAt);

        builder.Property(u => u.UpdatedAt);

        builder.HasIndex(u => u.Username)
            .IsUnique();

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.HasMany(u => u.UserRoles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
