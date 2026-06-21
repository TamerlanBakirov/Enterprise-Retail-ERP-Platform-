namespace GeorgiaERP.Application.Common;

public interface IAuthenticationService
{
    Task<AuthResult> LoginAsync(string username, string password, string? twoFactorCode, string? ipAddress, string? deviceInfo);
    Task<AuthResult> RefreshTokenAsync(string refreshToken, string? ipAddress);
    Task<bool> RevokeRefreshTokenAsync(string refreshToken, Guid? requestingUserId = null);
}

public record AuthResult(
    bool Success,
    string? AccessToken,
    string? RefreshToken,
    DateTimeOffset? ExpiresAt,
    string? Error);
