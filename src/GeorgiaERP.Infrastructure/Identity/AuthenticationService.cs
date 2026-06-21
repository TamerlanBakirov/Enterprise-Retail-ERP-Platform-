using System.Security.Cryptography;
using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Identity;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GeorgiaERP.Infrastructure.Identity;

public class AuthenticationService : IAuthenticationService
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordService _passwordService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly ITotpVerifier _totpVerifier;
    private readonly ITotpSecretProtector _totpSecretProtector;
    private readonly int _refreshTokenExpiryDays;
    private readonly int _maxFailedAttempts;
    private readonly TimeSpan _lockoutDuration;

    public AuthenticationService(
        AppDbContext dbContext,
        IPasswordService passwordService,
        IJwtTokenService jwtTokenService,
        ITotpVerifier totpVerifier,
        ITotpSecretProtector totpSecretProtector,
        IConfiguration configuration,
        ILogger<AuthenticationService> logger)
    {
        _dbContext = dbContext;
        _passwordService = passwordService;
        _jwtTokenService = jwtTokenService;
        _totpVerifier = totpVerifier;
        _totpSecretProtector = totpSecretProtector;
        _configuration = configuration;
        _logger = logger;
        _refreshTokenExpiryDays = int.TryParse(_configuration["Jwt:RefreshTokenExpiryDays"], out var days) ? days : 7;
        _maxFailedAttempts = int.TryParse(_configuration["Authentication:MaxFailedAttempts"], out var attempts) ? attempts : 5;
        _lockoutDuration = TimeSpan.FromMinutes(
            int.TryParse(_configuration["Authentication:LockoutMinutes"], out var minutes) ? minutes : 15);
    }

    public async Task<AuthResult> LoginAsync(string username, string password, string? twoFactorCode, string? ipAddress, string? deviceInfo)
    {
        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user is null)
        {
            _logger.LogWarning("Login attempt for non-existent user: {Username}", username);
            return new AuthResult(false, null, null, null, "Invalid username or password.");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login attempt for inactive user: {UserId}", user.Id);
            return new AuthResult(false, null, null, null, "Account is deactivated.");
        }

        if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTimeOffset.UtcNow)
        {
            _logger.LogWarning("Login attempt for locked user: {UserId}, locked until {LockedUntil}", user.Id, user.LockedUntil.Value);
            return new AuthResult(false, null, null, null, "Account is temporarily locked. Please try again later.");
        }

        if (!_passwordService.VerifyPassword(password, user.PasswordHash))
        {
            user.RecordFailedLogin(_maxFailedAttempts, _lockoutDuration);
            await _dbContext.SaveChangesAsync();
            _logger.LogWarning("Failed login attempt for user: {UserId}", user.Id);
            return new AuthResult(false, null, null, null, "Invalid username or password.");
        }

        if (user.Is2FaEnabled &&
            (string.IsNullOrWhiteSpace(user.TotpSecret) || string.IsNullOrWhiteSpace(twoFactorCode) ||
             !VerifyTwoFactorSecret(user.TotpSecret, twoFactorCode)))
        {
            user.RecordFailedLogin(_maxFailedAttempts, _lockoutDuration);
            await _dbContext.SaveChangesAsync();
            _logger.LogWarning("Invalid two-factor code for user: {UserId}", user.Id);
            return new AuthResult(false, null, null, null, "Invalid two-factor authentication code.");
        }

        if (user.Is2FaEnabled && user.TotpSecret is not null &&
            !user.TotpSecret.StartsWith("v1.", StringComparison.Ordinal))
            user.ReplaceTwoFactorSecret(_totpSecretProtector.Protect(user.TotpSecret));

        user.RecordSuccessfulLogin();

        var roles = user.UserRoles
            .Select(ur => ur.Role.Code)
            .Distinct()
            .ToList();

        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => $"{rp.Permission.Module}:{rp.Permission.Action}:{rp.Permission.Resource}")
            .Distinct()
            .ToList();

        // Generate tokens
        var accessToken = _jwtTokenService.GenerateAccessToken(user, roles, permissions, companyId: null);

        var rawRefreshToken = GenerateRawRefreshToken();
        var refreshTokenHash = JwtTokenService.HashToken(rawRefreshToken);

        var refreshTokenEntity = RefreshToken.Create(
            userId: user.Id,
            tokenHash: refreshTokenHash,
            expiresAt: DateTimeOffset.UtcNow.AddDays(_refreshTokenExpiryDays),
            deviceInfo: deviceInfo,
            ipAddress: ipAddress);

        _dbContext.RefreshTokens.Add(refreshTokenEntity);
        await _dbContext.SaveChangesAsync();

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(
            int.TryParse(_configuration["Jwt:AccessTokenExpiryMinutes"], out var expMin) ? expMin : 15);

        _logger.LogInformation("User {UserId} logged in successfully", user.Id);

        return new AuthResult(
            Success: true,
            AccessToken: accessToken,
            RefreshToken: rawRefreshToken,
            ExpiresAt: expiresAt,
            Error: null);
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken, string? ipAddress)
    {
        var tokenHash = JwtTokenService.HashToken(refreshToken);

        var storedToken = await _dbContext.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

        if (storedToken is null)
        {
            _logger.LogWarning("Refresh token not found");
            return new AuthResult(false, null, null, null, "Invalid refresh token.");
        }

        if (storedToken.RevokedAt.HasValue)
        {
            _logger.LogWarning("Attempt to use revoked refresh token for user: {UserId}", storedToken.UserId);
            return new AuthResult(false, null, null, null, "Refresh token has been revoked.");
        }

        if (storedToken.ExpiresAt < DateTimeOffset.UtcNow)
        {
            _logger.LogWarning("Attempt to use expired refresh token for user: {UserId}", storedToken.UserId);
            return new AuthResult(false, null, null, null, "Refresh token has expired.");
        }

        var user = storedToken.User;

        if (!user.IsActive)
        {
            _logger.LogWarning("Refresh token used for inactive user: {UserId}", user.Id);
            return new AuthResult(false, null, null, null, "Account is deactivated.");
        }

        // Revoke the old refresh token (token rotation)
        // Use EF Core to update RevokedAt via entry since setter is private
        _dbContext.Entry(storedToken).Property("RevokedAt").CurrentValue = DateTimeOffset.UtcNow;

        var roles = user.UserRoles
            .Select(ur => ur.Role.Code)
            .Distinct()
            .ToList();

        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => $"{rp.Permission.Module}:{rp.Permission.Action}:{rp.Permission.Resource}")
            .Distinct()
            .ToList();

        // Generate new tokens
        var accessToken = _jwtTokenService.GenerateAccessToken(user, roles, permissions, companyId: null);

        var newRawRefreshToken = GenerateRawRefreshToken();
        var newRefreshTokenHash = JwtTokenService.HashToken(newRawRefreshToken);

        var newRefreshTokenEntity = RefreshToken.Create(
            userId: user.Id,
            tokenHash: newRefreshTokenHash,
            expiresAt: DateTimeOffset.UtcNow.AddDays(_refreshTokenExpiryDays),
            deviceInfo: storedToken.DeviceInfo,
            ipAddress: ipAddress);

        _dbContext.RefreshTokens.Add(newRefreshTokenEntity);
        await _dbContext.SaveChangesAsync();

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(
            int.TryParse(_configuration["Jwt:AccessTokenExpiryMinutes"], out var expMin) ? expMin : 15);

        _logger.LogInformation("Refresh token rotated for user: {UserId}", user.Id);

        return new AuthResult(
            Success: true,
            AccessToken: accessToken,
            RefreshToken: newRawRefreshToken,
            ExpiresAt: expiresAt,
            Error: null);
    }

    public async Task<bool> RevokeRefreshTokenAsync(string refreshToken, Guid? requestingUserId = null)
    {
        var tokenHash = JwtTokenService.HashToken(refreshToken);

        var storedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

        if (storedToken is null)
        {
            _logger.LogWarning("Attempt to revoke non-existent refresh token");
            return false;
        }

        // Verify ownership: prevent users from revoking tokens that belong to other users
        if (requestingUserId.HasValue && storedToken.UserId != requestingUserId.Value)
        {
            _logger.LogWarning(
                "User {RequestingUserId} attempted to revoke a refresh token belonging to user {TokenOwner}",
                requestingUserId.Value, storedToken.UserId);
            return false;
        }

        if (storedToken.RevokedAt.HasValue)
        {
            return true;
        }

        _dbContext.Entry(storedToken).Property("RevokedAt").CurrentValue = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Refresh token revoked for user: {UserId}", storedToken.UserId);
        return true;
    }

    private static string GenerateRawRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }

    private bool VerifyTwoFactorSecret(string protectedSecret, string code)
    {
        try
        {
            return _totpVerifier.Verify(_totpSecretProtector.Unprotect(protectedSecret), code, DateTimeOffset.UtcNow);
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            _logger.LogError(ex, "Stored TOTP secret could not be decrypted");
            return false;
        }
    }
}
