namespace GeorgiaERP.Desktop.Models;

public record UserListDto(
    Guid Id,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    string? FirstNameKa,
    string? LastNameKa,
    string? Phone,
    bool IsActive,
    IReadOnlyList<string> Roles,
    DateTimeOffset CreatedAt);

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
