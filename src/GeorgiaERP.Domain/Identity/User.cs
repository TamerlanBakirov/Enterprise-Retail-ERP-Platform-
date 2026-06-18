using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Identity;

public class User : BaseEntity
{
    public string Username { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string? FirstNameKa { get; private set; }
    public string? LastNameKa { get; private set; }
    public string? Phone { get; private set; }
    public Guid? DefaultStoreId { get; private set; }
    public string DefaultLanguage { get; private set; } = "ka";
    public bool Is2FaEnabled { get; private set; }
    public string? TotpSecret { get; private set; }
    public int FailedLoginCount { get; private set; }
    public DateTimeOffset? LockedUntil { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Navigation properties
    public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();

    private User() { }

    public static User Create(
        string username,
        string email,
        string passwordHash,
        string firstName,
        string lastName,
        string? firstNameKa = null,
        string? lastNameKa = null)
    {
        return new User
        {
            Username = username,
            Email = email,
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            FirstNameKa = firstNameKa,
            LastNameKa = lastNameKa,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
