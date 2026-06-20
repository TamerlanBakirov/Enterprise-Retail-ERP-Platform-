using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Identity.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Identity.Commands;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly IAuthenticationService _authService;
    private readonly IAppDbContext _dbContext;

    public LoginCommandHandler(IAuthenticationService authService, IAppDbContext dbContext)
    {
        _authService = authService;
        _dbContext = dbContext;
    }

    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var authResult = await _authService.LoginAsync(
            request.Username,
            request.Password,
            request.TwoFactorCode,
            request.IpAddress,
            request.DeviceInfo);

        if (!authResult.Success)
            return Result.Failure<AuthResponse>(authResult.Error ?? "Authentication failed.");

        var user = await _dbContext.Users
            .Where(u => u.Username == request.Username)
            .Select(u => new UserInfo(
                u.Id,
                u.Username,
                u.Email,
                u.FirstName,
                u.LastName,
                u.FirstNameKa,
                u.LastNameKa,
                u.DefaultLanguage,
                u.UserRoles.Select(ur => ur.Role.Code).ToList(),
                u.DefaultStoreId))
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return Result.Failure<AuthResponse>("User not found.");

        var response = new AuthResponse(
            authResult.AccessToken!,
            authResult.RefreshToken!,
            authResult.ExpiresAt!.Value,
            user);

        return Result.Success(response);
    }
}
