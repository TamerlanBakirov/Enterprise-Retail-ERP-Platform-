using System.Security.Claims;
using GeorgiaERP.Domain.Identity;

namespace GeorgiaERP.Infrastructure.Identity;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles, IEnumerable<string> permissions, Guid? companyId);
    RefreshToken GenerateRefreshToken(Guid userId, string? deviceInfo, string? ipAddress);
    ClaimsPrincipal? ValidateToken(string token);
}
