using GeorgiaERP.Application.Licensing;

namespace GeorgiaERP.Infrastructure.Licensing;

/// <summary>
/// Always returns a valid license in Development environment.
/// Registered instead of LocalLicenseValidator when ASPNETCORE_ENVIRONMENT is Development.
/// </summary>
public sealed class DevelopmentLicenseValidator : ILicenseValidator
{
    public Task<LicenseInfo> ValidateAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new LicenseInfo(
            IsValid: true,
            CompanyName: "Development License",
            ExpiresAt: DateTimeOffset.UtcNow.AddYears(10),
            MaxUsers: 999,
            MaxStores: 999,
            Error: null));
    }
}
