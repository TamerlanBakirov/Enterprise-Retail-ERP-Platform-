namespace GeorgiaERP.Application.Licensing;

public interface ILicenseKeyValidator
{
    LicenseKeyValidationResult Validate(string licenseKey);
}

public record LicenseKeyValidationResult(
    bool IsValid,
    string? CompanyName,
    DateTimeOffset? ExpiresAt,
    int MaxUsers,
    int MaxStores,
    string? Error)
{
    public static LicenseKeyValidationResult Invalid(string error) => new(false, null, null, 0, 0, error);
}
