namespace GeorgiaERP.Desktop.Models;

public record LoginRequest(string Username, string Password, string? TwoFactorCode = null);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    UserInfo User);

public record UserInfo(
    Guid Id,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    string? FirstNameKa,
    string? LastNameKa,
    string DefaultLanguage,
    IReadOnlyList<string> Roles,
    Guid? DefaultStoreId)
{
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Role => Roles.FirstOrDefault() ?? string.Empty;
}

public record RefreshTokenRequest(string RefreshToken);

public record TwoFactorSetupResponse(string SharedKey, string? QrCodeUri);
public record TwoFactorConfirmRequest(string Code);
public record TwoFactorDisableRequest(string Code);
