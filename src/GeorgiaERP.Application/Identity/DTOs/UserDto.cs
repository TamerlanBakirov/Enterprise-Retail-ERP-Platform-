namespace GeorgiaERP.Application.Identity.DTOs;

public record UserDto(
    Guid Id,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    string? FirstNameKa,
    string? LastNameKa,
    string? Phone,
    Guid? DefaultStoreId,
    string DefaultLanguage,
    bool Is2FaEnabled,
    bool IsActive,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt,
    IReadOnlyList<string> Roles);

public record CreateUserRequest(
    string Username,
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? FirstNameKa,
    string? LastNameKa,
    string? Phone,
    Guid? DefaultStoreId,
    string DefaultLanguage,
    List<Guid> RoleIds);

public record UpdateUserRequest(
    string? Email,
    string? FirstName,
    string? LastName,
    string? FirstNameKa,
    string? LastNameKa,
    string? Phone,
    Guid? DefaultStoreId,
    string? DefaultLanguage,
    bool? IsActive);
