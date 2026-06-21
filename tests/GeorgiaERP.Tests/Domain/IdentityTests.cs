using FluentAssertions;
using GeorgiaERP.Domain.Identity;
using Xunit;

namespace GeorgiaERP.Tests.Domain;

public class IdentityTests
{
    [Fact]
    public void CreateUser_SetsDefaultValues()
    {
        var user = User.Create("admin", "admin@erp.ge", "hashed_pw", "Giorgi", "Beridze",
            "გიორგი", "ბერიძე", "+995555123456");

        user.Username.Should().Be("admin");
        user.Email.Should().Be("admin@erp.ge");
        user.PasswordHash.Should().Be("hashed_pw");
        user.FirstName.Should().Be("Giorgi");
        user.LastName.Should().Be("Beridze");
        user.FirstNameKa.Should().Be("გიორგი");
        user.LastNameKa.Should().Be("ბერიძე");
        user.Phone.Should().Be("+995555123456");
        user.DefaultLanguage.Should().Be("ka");
        user.IsActive.Should().BeTrue();
        user.Is2FaEnabled.Should().BeFalse();
        user.FailedLoginCount.Should().Be(0);
        user.LockedUntil.Should().BeNull();
        user.LastLoginAt.Should().BeNull();
    }

    [Fact]
    public void User_RecordFailedLogin_IncreasesCount()
    {
        var user = User.Create("test", "test@test.ge", "hash", "Test", "User");

        user.RecordFailedLogin(5, TimeSpan.FromMinutes(15));

        user.FailedLoginCount.Should().Be(1);
        user.LockedUntil.Should().BeNull(); // Not locked yet
    }

    [Fact]
    public void User_RecordFailedLogin_LocksAfterMaxAttempts()
    {
        var user = User.Create("test", "test@test.ge", "hash", "Test", "User");

        for (int i = 0; i < 5; i++)
            user.RecordFailedLogin(5, TimeSpan.FromMinutes(15));

        user.FailedLoginCount.Should().Be(5);
        user.LockedUntil.Should().NotBeNull();
        user.LockedUntil.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void User_RecordSuccessfulLogin_ResetsCounterAndLock()
    {
        var user = User.Create("test", "test@test.ge", "hash", "Test", "User");
        for (int i = 0; i < 5; i++)
            user.RecordFailedLogin(5, TimeSpan.FromMinutes(15));

        user.RecordSuccessfulLogin();

        user.FailedLoginCount.Should().Be(0);
        user.LockedUntil.Should().BeNull();
        user.LastLoginAt.Should().NotBeNull();
    }

    [Fact]
    public void User_EnableTwoFactor_SetsSecretAndFlag()
    {
        var user = User.Create("test", "test@test.ge", "hash", "Test", "User");

        user.EnableTwoFactor("BASE32SECRET");

        user.Is2FaEnabled.Should().BeTrue();
        user.TotpSecret.Should().Be("BASE32SECRET");
    }

    [Fact]
    public void User_EnableTwoFactor_WithEmptySecret_Throws()
    {
        var user = User.Create("test", "test@test.ge", "hash", "Test", "User");

        var act = () => user.EnableTwoFactor("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void User_BeginTwoFactorSetup_SetsSecretWithout2FaEnabled()
    {
        var user = User.Create("test", "test@test.ge", "hash", "Test", "User");

        user.BeginTwoFactorSetup("PROVISIONAL_SECRET");

        user.TotpSecret.Should().Be("PROVISIONAL_SECRET");
        user.Is2FaEnabled.Should().BeFalse(); // Not confirmed yet
    }

    [Fact]
    public void User_ConfirmTwoFactorSetup_EnablesAfterBegin()
    {
        var user = User.Create("test", "test@test.ge", "hash", "Test", "User");
        user.BeginTwoFactorSetup("PROVISIONAL_SECRET");

        user.ConfirmTwoFactorSetup();

        user.Is2FaEnabled.Should().BeTrue();
    }

    [Fact]
    public void User_ConfirmTwoFactorSetup_WithoutBegin_Throws()
    {
        var user = User.Create("test", "test@test.ge", "hash", "Test", "User");

        var act = () => user.ConfirmTwoFactorSetup();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Two-factor setup has not been started*");
    }

    [Fact]
    public void User_DisableTwoFactor_ClearsSecretAndFlag()
    {
        var user = User.Create("test", "test@test.ge", "hash", "Test", "User");
        user.EnableTwoFactor("SECRET");

        user.DisableTwoFactor();

        user.Is2FaEnabled.Should().BeFalse();
        user.TotpSecret.Should().BeNull();
    }

    [Fact]
    public void User_ReplaceTwoFactorSecret_UpdatesSecret()
    {
        var user = User.Create("test", "test@test.ge", "hash", "Test", "User");
        user.EnableTwoFactor("OLD_SECRET");

        user.ReplaceTwoFactorSecret("NEW_ENCRYPTED_SECRET");

        user.TotpSecret.Should().Be("NEW_ENCRYPTED_SECRET");
    }

    [Fact]
    public void User_ReplaceTwoFactorSecret_EmptyString_Throws()
    {
        var user = User.Create("test", "test@test.ge", "hash", "Test", "User");

        var act = () => user.ReplaceTwoFactorSecret("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void User_Create_WithDefaultLanguage_SetsLanguage()
    {
        var user = User.Create("test", "test@test.ge", "hash", "Test", "User",
            defaultLanguage: "en");

        user.DefaultLanguage.Should().Be("en");
    }

    [Fact]
    public void User_Create_WithDefaultStore_SetsStoreId()
    {
        var storeId = Guid.NewGuid();
        var user = User.Create("test", "test@test.ge", "hash", "Test", "User",
            defaultStoreId: storeId);

        user.DefaultStoreId.Should().Be(storeId);
    }
}
