namespace GeorgiaERP.Infrastructure.Identity;

public interface IAuthenticationService
{
    Task<AuthResult> LoginAsync(string username, string password, string? ipAddress, string? deviceInfo);
    Task<AuthResult> RefreshTokenAsync(string refreshToken, string? ipAddress);
    Task RevokeRefreshTokenAsync(string refreshToken);
}

public record AuthResult(
    bool Success,
    string? AccessToken,
    string? RefreshToken,
    DateTimeOffset? ExpiresAt,
    string? Error);
