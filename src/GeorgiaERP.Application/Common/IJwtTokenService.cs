using System.Security.Claims;
using GeorgiaERP.Domain.Identity;

namespace GeorgiaERP.Application.Common;

/// <summary>
/// Application-layer abstraction for JWT token generation and validation.
/// Infrastructure provides the concrete implementation with signing keys
/// and token configuration.
/// </summary>
public interface IJwtTokenService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles, IEnumerable<string> permissions, Guid? companyId);
    RefreshToken GenerateRefreshToken(Guid userId, string? deviceInfo, string? ipAddress);
    ClaimsPrincipal? ValidateToken(string token);
}
