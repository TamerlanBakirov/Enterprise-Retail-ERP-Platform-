using FluentAssertions;
using GeorgiaERP.Application.CRM.Commands;
using GeorgiaERP.Domain.CRM;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GeorgiaERP.Tests.Application;

public class CrmCommandHandlerTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"crm-{Guid.NewGuid()}")
            .Options);

    // === CreateCustomer ===

    [Fact]
    public async Task CreateCustomer_Valid_ReturnsSuccess()
    {
        await using var db = NewContext();
        var handler = new CreateCustomerCommandHandler(db);

        var result = await handler.Handle(new CreateCustomerCommand(
            "John", "Doe", "ჯონ", "დოუ", "Acme Ltd", "123456789",
            "+995555123456", "john@acme.ge", null, "Male", true, true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CustomerNumber.Should().StartWith("C-");
        (await db.Customers.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateCustomer_DuplicatePhone_ReturnsFailure()
    {
        await using var db = NewContext();
        var existing = Customer.Create("C-EXIST", "Existing", "Customer");
        existing.SetContactInfo("+995555999999", null);
        db.Customers.Add(existing);
        await db.SaveChangesAsync();

        var handler = new CreateCustomerCommandHandler(db);
        var result = await handler.Handle(new CreateCustomerCommand(
            "New", "Customer", null, null, null, null,
            "+995555999999", null, null, null, false, false),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Phone");
    }

    [Fact]
    public async Task CreateCustomer_NullPhone_Succeeds()
    {
        await using var db = NewContext();
        var handler = new CreateCustomerCommandHandler(db);

        var result = await handler.Handle(new CreateCustomerCommand(
            "NoPhone", "Customer", null, null, null, null,
            null, "nophone@test.ge", null, null, false, false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    // === LoyaltyCommands ===

    [Fact]
    public async Task EarnLoyaltyPoints_ValidCustomer_ReturnsSuccess()
    {
        await using var db = NewContext();
        var customer = Customer.Create("C-LOYAL", "Loyal", "Customer");
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var handler = new EarnLoyaltyPointsCommandHandler(db);
        var result = await handler.Handle(
            new EarnLoyaltyPointsCommand(customer.Id, 150, "Purchase #1234"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var saved = await db.Customers.FindAsync(customer.Id);
        saved!.LoyaltyPoints.Should().Be(150);
        (await db.LoyaltyTransactions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task EarnLoyaltyPoints_NonExistentCustomer_ReturnsFailure()
    {
        await using var db = NewContext();
        var handler = new EarnLoyaltyPointsCommandHandler(db);

        var result = await handler.Handle(
            new EarnLoyaltyPointsCommand(Guid.NewGuid(), 100, "Test"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task RedeemLoyaltyPoints_SufficientBalance_ReturnsSuccess()
    {
        await using var db = NewContext();
        var customer = Customer.Create("C-REDEEM", "Redeem", "Customer");
        customer.AddPoints(500);
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var handler = new RedeemLoyaltyPointsCommandHandler(db);
        var result = await handler.Handle(
            new RedeemLoyaltyPointsCommand(customer.Id, 200, "Discount"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var saved = await db.Customers.FindAsync(customer.Id);
        saved!.LoyaltyPoints.Should().Be(300);
    }

    [Fact]
    public async Task RedeemLoyaltyPoints_InsufficientBalance_ReturnsFailure()
    {
        await using var db = NewContext();
        var customer = Customer.Create("C-INSUFF", "NoPoints", "Customer");
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var handler = new RedeemLoyaltyPointsCommandHandler(db);
        var result = await handler.Handle(
            new RedeemLoyaltyPointsCommand(customer.Id, 100, "Too much"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
