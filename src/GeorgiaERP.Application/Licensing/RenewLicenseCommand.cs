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

    public RenewLicenseCommandHandler(IAppDbContext dbContext, IMachineIdProvider machineIdProvider)
    {
        _dbContext = dbContext;
        _machineIdProvider = machineIdProvider;
    }

    public async Task<Result<LicenseActivationResponse>> Handle(RenewLicenseCommand request, CancellationToken cancellationToken)
    {
        var machineId = _machineIdProvider.GetMachineId();

        var license = await _dbContext.Licenses
            .FirstOrDefaultAsync(l => l.LicenseKey == request.LicenseKey && l.MachineId == machineId, cancellationToken);

        if (license is null)
            return Result.Failure<LicenseActivationResponse>("License not found for this machine.");

        if (license.Status == LicenseStatus.Revoked)
            return Result.Failure<LicenseActivationResponse>("This license has been revoked. Contact support.");

        var newExpiry = license.ExpiresAt > DateTimeOffset.UtcNow
            ? license.ExpiresAt.AddYears(1)
            : DateTimeOffset.UtcNow.AddYears(1);

        license.Renew(newExpiry);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success<LicenseActivationResponse>(new LicenseActivationResponse(
            true, license.CompanyName, license.ExpiresAt, license.MaxUsers, license.MaxStores));
    }
}
