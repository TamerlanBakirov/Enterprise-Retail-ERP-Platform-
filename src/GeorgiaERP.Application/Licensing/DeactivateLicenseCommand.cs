using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Licensing;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Licensing;

public record DeactivateLicenseCommand : IRequest<Result>;

public class DeactivateLicenseCommandHandler : IRequestHandler<DeactivateLicenseCommand, Result>
{
    private readonly IAppDbContext _dbContext;
    private readonly IMachineIdProvider _machineIdProvider;

    public DeactivateLicenseCommandHandler(IAppDbContext dbContext, IMachineIdProvider machineIdProvider)
    {
        _dbContext = dbContext;
        _machineIdProvider = machineIdProvider;
    }

    public async Task<Result> Handle(DeactivateLicenseCommand request, CancellationToken cancellationToken)
    {
        var machineId = _machineIdProvider.GetMachineId();

        var license = await _dbContext.Licenses
            .FirstOrDefaultAsync(l => l.MachineId == machineId && l.Status == LicenseStatus.Active, cancellationToken);

        if (license is null)
            return Result.Failure("No active license found on this machine.");

        license.Revoke();
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
