using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Identity.DTOs;
using MediatR;

namespace GeorgiaERP.Application.Identity.Commands;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private readonly IAuthenticationService _authService;

    public RefreshTokenCommandHandler(IAuthenticationService authService)
    {
        _authService = authService;
    }

    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var authResult = await _authService.RefreshTokenAsync(request.RefreshToken, request.IpAddress);

        if (!authResult.Success)
            return Result.Failure<AuthResponse>(authResult.Error ?? "Token refresh failed.");

        var response = new AuthResponse(
            authResult.AccessToken!,
            authResult.RefreshToken!,
            authResult.ExpiresAt!.Value,
            User: null!);

        return Result.Success(response);
    }
}
