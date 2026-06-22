using FluentAssertions;
using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.CRM.Commands;
using GeorgiaERP.Application.CRM.Queries;
using GeorgiaERP.Application.Identity.Commands;
using GeorgiaERP.Application.Identity.Queries;
using GeorgiaERP.Domain.CRM;
using GeorgiaERP.Domain.Identity;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace GeorgiaERP.Tests.Application;

public class UserCustomerCrudHandlerTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"user-cust-crud-{Guid.NewGuid()}")
            .Options);

    private static async Task<Guid> SeedUser(AppDbContext db, string username = "testuser", string email = "test@test.ge")
    {
        var user = User.Create(username, email, "hash", "Test", "User");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    private static async Task<Guid> SeedCustomer(AppDbContext db, string phone = "+995555000001")
    {
        var customer = Customer.Create("C-TEST-001", "Giorgi", "Beridze", "გიორგი", "ბერიძე");
        customer.SetContactInfo(phone, "giorgi@test.ge");
        customer.SetCompany("Test LLC", "123456789");
        customer.SetConsent(true, true);
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return customer.Id;
    }

    // === GetUserById ===

    [Fact]
    public async Task GetUserById_Exists_ReturnsUser()
    {
        await using var db = NewContext();
        var userId = await SeedUser(db);
        var handler = new GetUserByIdQueryHandler(db);

        var result = await handler.Handle(new GetUserByIdQuery(userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Username.Should().Be("testuser");
        result.Value.Email.Should().Be("test@test.ge");
    }

    [Fact]
    public async Task GetUserById_NotFound_ReturnsFailure()
    {
        await using var db = NewContext();
        var handler = new GetUserByIdQueryHandler(db);

        var result = await handler.Handle(new GetUserByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ErrorCodes.NotFound);
    }

    // === UpdateUser ===

    [Fact]
    public async Task UpdateUser_Valid_UpdatesFields()
    {
        await using var db = NewContext();
        var userId = await SeedUser(db);
        var handler = new UpdateUserCommandHandler(db);

        var result = await handler.Handle(new UpdateUserCommand(
            userId, "new@email.ge", "NewFirst", "NewLast",
            null, null, "+995555999999", null, "en", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var user = await db.Users.FindAsync(userId);
        user!.Email.Should().Be("new@email.ge");
        user.FirstName.Should().Be("NewFirst");
        user.Phone.Should().Be("+995555999999");
    }

    [Fact]
    public async Task UpdateUser_NotFound_ReturnsFailure()
    {
        await using var db = NewContext();
        var handler = new UpdateUserCommandHandler(db);

        var result = await handler.Handle(new UpdateUserCommand(
            Guid.NewGuid(), "a@b.com", null, null,
            null, null, null, null, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateUser_DuplicateEmail_ReturnsFailure()
    {
        await using var db = NewContext();
        await SeedUser(db, "user1", "user1@test.ge");
        var user2Id = await SeedUser(db, "user2", "user2@test.ge");
        var handler = new UpdateUserCommandHandler(db);

        var result = await handler.Handle(new UpdateUserCommand(
            user2Id, "user1@test.ge", null, null,
            null, null, null, null, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ErrorCodes.EmailTaken);
    }

    [Fact]
    public async Task UpdateUser_Deactivate_SetsInactive()
    {
        await using var db = NewContext();
        var userId = await SeedUser(db);
        var handler = new UpdateUserCommandHandler(db);

        await handler.Handle(new UpdateUserCommand(
            userId, null, null, null, null, null, null, null, null, false),
            CancellationToken.None);

        var user = await db.Users.FindAsync(userId);
        user!.IsActive.Should().BeFalse();
    }

    // === GetCustomerById ===

    [Fact]
    public async Task GetCustomerById_Exists_ReturnsCustomer()
    {
        await using var db = NewContext();
        var customerId = await SeedCustomer(db);
        var handler = new GetCustomerByIdQueryHandler(db);

        var result = await handler.Handle(new GetCustomerByIdQuery(customerId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.FirstName.Should().Be("Giorgi");
        result.Value.CompanyName.Should().Be("Test LLC");
    }

    [Fact]
    public async Task GetCustomerById_NotFound_ReturnsFailure()
    {
        await using var db = NewContext();
        var handler = new GetCustomerByIdQueryHandler(db);

        var result = await handler.Handle(new GetCustomerByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ErrorCodes.NotFound);
    }

    // === UpdateCustomer ===

    [Fact]
    public async Task UpdateCustomer_Valid_UpdatesFields()
    {
        await using var db = NewContext();
        var customerId = await SeedCustomer(db);
        var handler = new UpdateCustomerCommandHandler(db);

        var result = await handler.Handle(new UpdateCustomerCommand(
            customerId, null, null, null, null,
            "Updated LLC", "987654321",
            "+995555111111", "new@email.ge",
            false, true, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var customer = await db.Customers.FindAsync(customerId);
        customer!.CompanyName.Should().Be("Updated LLC");
        customer.Phone.Should().Be("+995555111111");
        customer.ConsentSms.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateCustomer_NotFound_ReturnsFailure()
    {
        await using var db = NewContext();
        var handler = new UpdateCustomerCommandHandler(db);

        var result = await handler.Handle(new UpdateCustomerCommand(
            Guid.NewGuid(), null, null, null, null,
            null, null, null, null, null, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateCustomer_Deactivate_SetsInactive()
    {
        await using var db = NewContext();
        var customerId = await SeedCustomer(db);
        var handler = new UpdateCustomerCommandHandler(db);

        await handler.Handle(new UpdateCustomerCommand(
            customerId, null, null, null, null,
            null, null, null, null, null, null, false),
            CancellationToken.None);

        var customer = await db.Customers.FindAsync(customerId);
        customer!.IsActive.Should().BeFalse();
    }
}
