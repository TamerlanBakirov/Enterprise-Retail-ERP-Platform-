using GeorgiaERP.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Identity.Commands;

public record UnlockAccountCommand(Guid UserId) : IRequest<Result>;

public class UnlockAccountCommandHandler : IRequestHandler<UnlockAccountCommand, Result>
{
    private readonly IAppDbContext _dbContext;

    public UnlockAccountCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(UnlockAccountCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            return Result.NotFound("User", request.UserId);

        user.Unlock();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
