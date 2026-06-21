using GeorgiaERP.Application.Common;
using MediatR;

namespace GeorgiaERP.Application.Identity.Commands;

public record RevokeRefreshTokenCommand(string RefreshToken, Guid? RequestingUserId = null) : IRequest<Result>;

public sealed class RevokeRefreshTokenCommandHandler : IRequestHandler<RevokeRefreshTokenCommand, Result>
{
    private readonly IAuthenticationService _authenticationService;
    public RevokeRefreshTokenCommandHandler(IAuthenticationService authenticationService) =>
        _authenticationService = authenticationService;

    public async Task<Result> Handle(RevokeRefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Result.Failure("Refresh token is required.");
        var revoked = await _authenticationService.RevokeRefreshTokenAsync(request.RefreshToken, request.RequestingUserId);
        return revoked ? Result.Success() : Result.Failure("Invalid or unauthorized refresh token.");
    }
}
