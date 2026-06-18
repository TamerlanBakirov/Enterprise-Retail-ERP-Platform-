using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Identity;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public string? DeviceInfo { get; private set; } // jsonb
    public string? IpAddress { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // Navigation properties
    public User User { get; private set; } = default!;

    private RefreshToken() { }

    public static RefreshToken Create(Guid userId, string tokenHash, DateTimeOffset expiresAt, string? deviceInfo = null, string? ipAddress = null)
    {
        return new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            DeviceInfo = deviceInfo,
            IpAddress = ipAddress,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
