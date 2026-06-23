using FluentAssertions;
using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Identity.Commands;
using GeorgiaERP.Domain.Identity;
using GeorgiaERP.Infrastructure.Identity;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace GeorgiaERP.Tests.Application;

public class SecurityCommandHandlerTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"security-{Guid.NewGuid()}")
            .Options);

    private static IPasswordService MockPasswordService()
    {
        var svc = Substitute.For<IPasswordService>();
        svc.HashPassword(Arg.Any<string>()).Returns(ci => $"hashed:{ci.Arg<string>()}");
        svc.VerifyPassword(Arg.Any<string>(), Arg.Any<string>())
            .Returns(ci => $"hashed:{ci.ArgAt<string>(0)}" == ci.ArgAt<string>(1));
        return svc;
    }

    private static async Task<Guid> SeedUser(AppDbContext db, string password = "OldPass123")
    {
        var svc = MockPasswordService();
        var user = User.Create("secuser", "sec@test.ge", svc.HashPassword(password), "Sec", "User");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    // === ChangePassword ===

    [Fact]
    public async Task ChangePassword_Valid_UpdatesHash()
    {
        await using var db = NewContext();
        var pwSvc = MockPasswordService();
        var userId = await SeedUser(db);
        var handler = new ChangePasswordCommandHandler(db, pwSvc);

        var result = await handler.Handle(new ChangePasswordCommand(
            userId, "OldPass123", "NewSecure456"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var user = await db.Users.FindAsync(userId);
        user!.PasswordHash.Should().Be("hashed:NewSecure456");
    }

    [Fact]
    public async Task ChangePassword_WrongCurrent_ReturnsFailure()
    {
        await using var db = NewContext();
        var pwSvc = MockPasswordService();
        var userId = await SeedUser(db);
        var handler = new ChangePasswordCommandHandler(db, pwSvc);

        var result = await handler.Handle(new ChangePasswordCommand(
            userId, "WrongPassword", "NewSecure456"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ErrorCodes.InvalidCredentials);
    }

    [Fact]
    public async Task ChangePassword_TooShort_ReturnsFailure()
    {
        await using var db = NewContext();
        var pwSvc = MockPasswordService();
        var userId = await SeedUser(db);
        var handler = new ChangePasswordCommandHandler(db, pwSvc);

        var result = await handler.Handle(new ChangePasswordCommand(
            userId, "OldPass123", "short"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ErrorCodes.ValidationError);
    }

    [Fact]
    public async Task ChangePassword_UserNotFound_ReturnsFailure()
    {
        await using var db = NewContext();
        var pwSvc = MockPasswordService();
        var handler = new ChangePasswordCommandHandler(db, pwSvc);

        var result = await handler.Handle(new ChangePasswordCommand(
            Guid.NewGuid(), "old", "newpassword123"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    // === AdminResetPassword ===

    [Fact]
    public async Task AdminReset_Valid_SetsNewHash()
    {
        await using var db = NewContext();
        var pwSvc = MockPasswordService();
        var userId = await SeedUser(db);
        var handler = new AdminResetPasswordCommandHandler(db, pwSvc);

        var result = await handler.Handle(new AdminResetPasswordCommand(
            userId, "AdminReset99"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var user = await db.Users.FindAsync(userId);
        user!.PasswordHash.Should().Be("hashed:AdminReset99");
    }

    [Fact]
    public async Task AdminReset_TooShort_ReturnsFailure()
    {
        await using var db = NewContext();
        var pwSvc = MockPasswordService();
        var userId = await SeedUser(db);
        var handler = new AdminResetPasswordCommandHandler(db, pwSvc);

        var result = await handler.Handle(new AdminResetPasswordCommand(
            userId, "abc"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task AdminReset_UserNotFound_ReturnsFailure()
    {
        await using var db = NewContext();
        var pwSvc = MockPasswordService();
        var handler = new AdminResetPasswordCommandHandler(db, pwSvc);

        var result = await handler.Handle(new AdminResetPasswordCommand(
            Guid.NewGuid(), "NewPass12345"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    // === UnlockAccount ===

    [Fact]
    public async Task Unlock_LockedUser_ClearsLockout()
    {
        await using var db = NewContext();
        var userId = await SeedUser(db);

        var user = await db.Users.FindAsync(userId);
        user!.RecordFailedLogin(5, TimeSpan.FromMinutes(30));
        user.RecordFailedLogin(5, TimeSpan.FromMinutes(30));
        user.RecordFailedLogin(5, TimeSpan.FromMinutes(30));
        user.RecordFailedLogin(5, TimeSpan.FromMinutes(30));
        user.RecordFailedLogin(5, TimeSpan.FromMinutes(30));
        await db.SaveChangesAsync();

        user.LockedUntil.Should().NotBeNull();

        var handler = new UnlockAccountCommandHandler(db);
        var result = await handler.Handle(new UnlockAccountCommand(userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        await db.Entry(user).ReloadAsync();
        user.LockedUntil.Should().BeNull();
        user.FailedLoginCount.Should().Be(0);
    }

    [Fact]
    public async Task Unlock_UserNotFound_ReturnsFailure()
    {
        await using var db = NewContext();
        var handler = new UnlockAccountCommandHandler(db);

        var result = await handler.Handle(new UnlockAccountCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
