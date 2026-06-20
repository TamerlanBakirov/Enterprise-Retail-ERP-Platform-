using GeorgiaERP.Application.Licensing;
using GeorgiaERP.Domain.Licensing;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Infrastructure.Licensing;

public class LocalLicenseValidator : ILicenseValidator
{
    private readonly AppDbContext _dbContext;

    public LocalLicenseValidator(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<LicenseInfo> ValidateAsync(CancellationToken cancellationToken = default)
    {
        var machineId = MachineIdProvider.GetMachineId();

        var license = await _dbContext.Set<License>()
            .FirstOrDefaultAsync(l => l.MachineId == machineId && l.Status == LicenseStatus.Active, cancellationToken);

        if (license is null)
            return new LicenseInfo(false, null, null, 0, 0, "No active license found for this machine.");

        license.RecordCheck();
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (!license.IsValid)
            return new LicenseInfo(false, license.CompanyName, license.ExpiresAt, 0, 0, "License has expired.");

        return new LicenseInfo(true, license.CompanyName, license.ExpiresAt, license.MaxUsers, license.MaxStores, null);
    }
}
