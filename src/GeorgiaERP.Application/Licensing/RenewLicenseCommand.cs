using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Licensing;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Licensing;

public record RenewLicenseCommand(string LicenseKey) : IRequest<Result<LicenseActivationResponse>>;

public class RenewLicenseCommandHandler : IRequestHandler<RenewLicenseCommand, Result<LicenseActivationResponse>>
{
    private readonly IAppDbContext _dbContext;
    private readonly IMachineIdProvider _machineIdProvider;
    private readonly ILicenseKeyValidator _licenseKeyValidator;

    public RenewLicenseCommandHandler(IAppDbContext dbContext, IMachineIdProvider machineIdProvider, ILicenseKeyValidator licenseKeyValidator)
    {
        _dbContext = dbContext;
        _machineIdProvider = machineIdProvider;
        _licenseKeyValidator = licenseKeyValidator;
    }

    public async Task<Result<LicenseActivationResponse>> Handle(RenewLicenseCommand request, CancellationToken cancellationToken)
    {
        var machineId = _machineIdProvider.GetMachineId();

        var validation = _licenseKeyValidator.Validate(request.LicenseKey);
        if (!validation.IsValid)
            return Result.Failure<LicenseActivationResponse>(validation.Error ?? "Invalid renewal key.");

        var license = await _dbContext.Licenses
            .FirstOrDefaultAsync(l => l.MachineId == machineId && l.Status != LicenseStatus.Revoked, cancellationToken);

        if (license is null)
            return Result.Failure<LicenseActivationResponse>("License not found for this machine.");

        if (license.Status == LicenseStatus.Revoked)
            return Result.Failure<LicenseActivationResponse>("This license has been revoked. Contact support.");

        if (!string.Equals(license.CompanyName, validation.CompanyName, StringComparison.OrdinalIgnoreCase))
            return Result.Failure<LicenseActivationResponse>("Renewal key was issued for a different company.");
        if (validation.ExpiresAt <= license.ExpiresAt)
            return Result.Failure<LicenseActivationResponse>("Renewal key does not extend the current license.");

        license.Renew(request.LicenseKey, validation.ExpiresAt!.Value, validation.MaxUsers, validation.MaxStores);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success<LicenseActivationResponse>(new LicenseActivationResponse(
            true, license.CompanyName, license.ExpiresAt, license.MaxUsers, license.MaxStores));
    }
}
