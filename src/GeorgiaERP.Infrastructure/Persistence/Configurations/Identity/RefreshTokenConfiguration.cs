using GeorgiaERP.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Identity;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.UserId)
            .IsRequired();

        builder.Property(rt => rt.TokenHash)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(rt => rt.DeviceInfo)
            .HasMaxLength(500);

        builder.Property(rt => rt.IpAddress)
            .HasMaxLength(45);

        builder.Property(rt => rt.ExpiresAt);

        builder.Property(rt => rt.RevokedAt);

        builder.Property(rt => rt.CreatedAt);

        // Token hash must be unique for security
        builder.HasIndex(rt => rt.TokenHash)
            .IsUnique()
            .HasDatabaseName("IX_refresh_tokens_token_hash");

        builder.HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // FK index for user token lookups
        builder.HasIndex(rt => rt.UserId)
            .HasDatabaseName("IX_refresh_tokens_user");

        // Expiry cleanup - find expired tokens for purging
        builder.HasIndex(rt => rt.ExpiresAt)
            .HasDatabaseName("IX_refresh_tokens_expires_at");
    }
}
