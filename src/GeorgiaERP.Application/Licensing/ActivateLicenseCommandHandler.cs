using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Licensing;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Licensing;

public class ActivateLicenseCommandHandler : IRequestHandler<ActivateLicenseCommand, Result<LicenseActivationResponse>>
{
    private readonly IAppDbContext _dbContext;
    private readonly IMachineIdProvider _machineIdProvider;
    private readonly ILicenseKeyValidator _licenseKeyValidator;

    public ActivateLicenseCommandHandler(IAppDbContext dbContext, IMachineIdProvider machineIdProvider, ILicenseKeyValidator licenseKeyValidator)
    {
        _dbContext = dbContext;
        _machineIdProvider = machineIdProvider;
        _licenseKeyValidator = licenseKeyValidator;
    }

    public async Task<Result<LicenseActivationResponse>> Handle(ActivateLicenseCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.LicenseKey))
            return Result.Failure<LicenseActivationResponse>("License key is required.");

        if (string.IsNullOrWhiteSpace(request.CompanyName))
            return Result.Failure<LicenseActivationResponse>("Company name is required.");

        var validation = _licenseKeyValidator.Validate(request.LicenseKey);
        if (!validation.IsValid)
            return Result.Failure<LicenseActivationResponse>(validation.Error ?? "Invalid license key.");
        if (!string.Equals(validation.CompanyName, request.CompanyName.Trim(), StringComparison.OrdinalIgnoreCase))
            return Result.Failure<LicenseActivationResponse>("License key was issued for a different company.");

        var machineId = _machineIdProvider.GetMachineId();

        var existing = await _dbContext.Licenses
            .FirstOrDefaultAsync(l => l.MachineId == machineId && l.Status == LicenseStatus.Active, cancellationToken);

        if (existing is not null)
        {
            return Result.Success<LicenseActivationResponse>(new LicenseActivationResponse(
                true, existing.CompanyName, existing.ExpiresAt, existing.MaxUsers, existing.MaxStores));
        }

        var duplicate = await _dbContext.Licenses
            .AnyAsync(l => l.LicenseKey == request.LicenseKey && l.Status == LicenseStatus.Active, cancellationToken);

        if (duplicate)
            return Result.Failure<LicenseActivationResponse>("This license key is already activated on another machine.");

        var revokedOrSuspended = await _dbContext.Licenses
            .FirstOrDefaultAsync(l => l.LicenseKey == request.LicenseKey
                && (l.Status == LicenseStatus.Revoked || l.Status == LicenseStatus.Suspended), cancellationToken);

        if (revokedOrSuspended is not null)
            return Result.Failure<LicenseActivationResponse>($"This license key has been {revokedOrSuspended.Status.ToString().ToLower()}.");

        var license = License.Create(
            request.LicenseKey,
            validation.CompanyName!,
            machineId,
            expiresAt: validation.ExpiresAt!.Value,
            maxUsers: validation.MaxUsers,
            maxStores: validation.MaxStores);

        if (!string.IsNullOrWhiteSpace(request.ContactEmail))
            license.SetContactEmail(request.ContactEmail);

        _dbContext.Licenses.Add(license);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success<LicenseActivationResponse>(new LicenseActivationResponse(
            true, license.CompanyName, license.ExpiresAt, license.MaxUsers, license.MaxStores));
    }
}
