namespace GeorgiaERP.Desktop.Models;

public record LoginRequest(string Username, string Password);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    UserInfo User);

public record UserInfo(
    Guid Id,
    string Username,
    string FullName,
    string Role,
    Guid? StoreId,
    string? StoreName);

public record RefreshTokenRequest(string RefreshToken);
