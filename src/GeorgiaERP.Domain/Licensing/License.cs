using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Licensing;

public enum LicenseStatus
{
    Active,
    Expired,
    Suspended,
    Revoked
}

public class License : BaseEntity
{
    public string LicenseKey { get; private set; } = default!;
    public string CompanyName { get; private set; } = default!;
    public string? ContactEmail { get; private set; }
    public string MachineId { get; private set; } = default!;
    public LicenseStatus Status { get; private set; }
    public DateTimeOffset ActivatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? LastCheckedAt { get; private set; }
    public int MaxUsers { get; private set; }
    public int MaxStores { get; private set; }
    public string? Features { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private License() { }

    public static License Create(
        string licenseKey, string companyName, string machineId,
        DateTimeOffset expiresAt, int maxUsers = 5, int maxStores = 1)
    {
        return new License
        {
            LicenseKey = licenseKey,
            CompanyName = companyName,
            MachineId = machineId,
            Status = LicenseStatus.Active,
            ActivatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt,
            MaxUsers = maxUsers,
            MaxStores = maxStores,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public bool IsValid => Status == LicenseStatus.Active && ExpiresAt > DateTimeOffset.UtcNow;
    public void RecordCheck() => LastCheckedAt = DateTimeOffset.UtcNow;
    public void Suspend() => Status = LicenseStatus.Suspended;
    public void Revoke() => Status = LicenseStatus.Revoked;
    public void Renew(string newLicenseKey, DateTimeOffset newExpiry, int maxUsers, int maxStores)
    {
        if (newExpiry <= ExpiresAt)
            throw new InvalidOperationException("A renewal must extend the current expiry date.");
        LicenseKey = newLicenseKey;
        ExpiresAt = newExpiry;
        MaxUsers = maxUsers;
        MaxStores = maxStores;
        Status = LicenseStatus.Active;
    }
    public void SetContactEmail(string email) => ContactEmail = email;
    public void SetFeatures(string features) => Features = features;
}
