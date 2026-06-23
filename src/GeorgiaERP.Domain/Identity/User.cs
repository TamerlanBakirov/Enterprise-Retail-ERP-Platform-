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

    public void RecordFailedLogin(int maxAttempts, TimeSpan lockoutDuration)
    {
        FailedLoginCount++;
        if (FailedLoginCount >= maxAttempts)
            LockedUntil = DateTimeOffset.UtcNow.Add(lockoutDuration);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RecordSuccessfulLogin()
    {
        FailedLoginCount = 0;
        LockedUntil = null;
        LastLoginAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void EnableTwoFactor(string totpSecret)
    {
        if (string.IsNullOrWhiteSpace(totpSecret))
            throw new ArgumentException("TOTP secret is required.", nameof(totpSecret));
        TotpSecret = totpSecret;
        Is2FaEnabled = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void BeginTwoFactorSetup(string totpSecret)
    {
        if (string.IsNullOrWhiteSpace(totpSecret))
            throw new ArgumentException("TOTP secret is required.", nameof(totpSecret));
        TotpSecret = totpSecret;
        Is2FaEnabled = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ConfirmTwoFactorSetup()
    {
        if (string.IsNullOrWhiteSpace(TotpSecret))
            throw new InvalidOperationException("Two-factor setup has not been started.");
        Is2FaEnabled = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ReplaceTwoFactorSecret(string protectedSecret)
    {
        if (string.IsNullOrWhiteSpace(protectedSecret))
            throw new ArgumentException("Protected TOTP secret is required.", nameof(protectedSecret));
        TotpSecret = protectedSecret;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void DisableTwoFactor()
    {
        TotpSecret = null;
        Is2FaEnabled = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        PasswordHash = passwordHash;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Unlock()
    {
        FailedLoginCount = 0;
        LockedUntil = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(
        string? email = null,
        string? firstName = null,
        string? lastName = null,
        string? firstNameKa = null,
        string? lastNameKa = null,
        string? phone = null,
        Guid? defaultStoreId = null,
        string? defaultLanguage = null,
        bool? isActive = null)
    {
        if (email is not null) Email = email;
        if (firstName is not null) FirstName = firstName;
        if (lastName is not null) LastName = lastName;
        if (firstNameKa is not null) FirstNameKa = firstNameKa;
        if (lastNameKa is not null) LastNameKa = lastNameKa;
        if (phone is not null) Phone = phone;
        if (defaultStoreId.HasValue) DefaultStoreId = defaultStoreId;
        if (defaultLanguage is not null) DefaultLanguage = defaultLanguage;
        if (isActive.HasValue) IsActive = isActive.Value;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private User() { }

    public static User Create(
        string username,
        string email,
        string passwordHash,
        string firstName,
        string lastName,
        string? firstNameKa = null,
        string? lastNameKa = null,
        string? phone = null,
        Guid? defaultStoreId = null,
        string defaultLanguage = "ka")
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
            Phone = phone,
            DefaultStoreId = defaultStoreId,
            DefaultLanguage = defaultLanguage,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
