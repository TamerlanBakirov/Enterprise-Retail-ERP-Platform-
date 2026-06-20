namespace GeorgiaERP.Application.Identity.DTOs;

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
    Guid? DefaultStoreId);
