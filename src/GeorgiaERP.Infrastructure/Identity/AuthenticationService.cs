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
    private readonly int _refreshTokenExpiryDays;

    public AuthenticationService(
        AppDbContext dbContext,
        IPasswordService passwordService,
        IJwtTokenService jwtTokenService,
        IConfiguration configuration,
        ILogger<AuthenticationService> logger)
    {
        _dbContext = dbContext;
        _passwordService = passwordService;
        _jwtTokenService = jwtTokenService;
        _configuration = configuration;
        _logger = logger;
        _refreshTokenExpiryDays = int.TryParse(_configuration["Jwt:RefreshTokenExpiryDays"], out var days) ? days : 7;
    }

    public async Task<AuthResult> LoginAsync(string username, string password, string? ipAddress, string? deviceInfo)
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
            _logger.LogWarning("Failed login attempt for user: {UserId}", user.Id);
            return new AuthResult(false, null, null, null, "Invalid username or password.");
        }

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

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        var tokenHash = JwtTokenService.HashToken(refreshToken);

        var storedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

        if (storedToken is null)
        {
            _logger.LogWarning("Attempt to revoke non-existent refresh token");
            return;
        }

        if (storedToken.RevokedAt.HasValue)
        {
            return;
        }

        _dbContext.Entry(storedToken).Property("RevokedAt").CurrentValue = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Refresh token revoked for user: {UserId}", storedToken.UserId);
    }

    private static string GenerateRawRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }
}
