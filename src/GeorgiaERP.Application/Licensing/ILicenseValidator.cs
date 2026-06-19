namespace GeorgiaERP.Application.Licensing;

public interface ILicenseValidator
{
    Task<LicenseInfo> ValidateAsync(CancellationToken cancellationToken = default);
}

public record LicenseInfo(
    bool IsValid,
    string? CompanyName,
    DateTimeOffset? ExpiresAt,
    int MaxUsers,
    int MaxStores,
    string? Error);
