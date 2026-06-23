using GeorgiaERP.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Identity.Commands;

public record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword) : IRequest<Result>;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly IAppDbContext _dbContext;
    private readonly IPasswordService _passwordService;

    public ChangePasswordCommandHandler(IAppDbContext dbContext, IPasswordService passwordService)
    {
        _dbContext = dbContext;
        _passwordService = passwordService;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            return Result.NotFound("User", request.UserId);

        if (!_passwordService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            return Result.Failure("Current password is incorrect.", ErrorCodes.InvalidCredentials);

        if (request.NewPassword.Length < 8)
            return Result.Failure("New password must be at least 8 characters.", ErrorCodes.ValidationError);

        var newHash = _passwordService.HashPassword(request.NewPassword);
        user.SetPasswordHash(newHash);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
