using FluentAssertions;
using GeorgiaERP.Application.CRM.Commands;
using GeorgiaERP.Domain.CRM;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GeorgiaERP.Tests.Application;

public class ExpireLoyaltyPointsHandlerTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"loyalty-expire-{Guid.NewGuid()}")
            .Options);

    private static void SetCreatedAt(LoyaltyTransaction tx, DateTimeOffset when) =>
        tx.GetType().GetProperty("CreatedAt")!.SetValue(tx, when);

    private static async Task<Customer> SeedCustomerWithPoints(
        AppDbContext db, string code, int points, DateTimeOffset lastActivity)
    {
        var customer = Customer.Create(code, "Test", "Customer");
        customer.AddPoints(points);
        db.Customers.Add(customer);
        var tx = LoyaltyTransaction.Create(customer.Id, LoyaltyTransactionType.Earn, points, points, "seed");
        SetCreatedAt(tx, lastActivity);
        db.LoyaltyTransactions.Add(tx);
        await db.SaveChangesAsync();
        return customer;
    }

    [Fact]
    public async Task Expire_InactiveCustomer_ZeroesBalanceAndWritesLedgerRow()
    {
        await using var db = NewContext();
        var stale = await SeedCustomerWithPoints(db, "C-OLD", 500, DateTimeOffset.UtcNow.AddMonths(-18));

        var result = await new ExpireLoyaltyPointsCommandHandler(db)
            .Handle(new ExpireLoyaltyPointsCommand(InactivityMonths: 12), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CustomersAffected.Should().Be(1);
        result.Value.PointsExpired.Should().Be(500);

        (await db.Customers.FindAsync(stale.Id))!.LoyaltyPoints.Should().Be(0);
        var expireRow = await db.LoyaltyTransactions
            .SingleAsync(t => t.CustomerId == stale.Id && t.TransactionType == LoyaltyTransactionType.Expire);
        expireRow.Points.Should().Be(-500);
        expireRow.BalanceAfter.Should().Be(0);
    }

    [Fact]
    public async Task Expire_ActiveCustomer_IsUntouched()
    {
        await using var db = NewContext();
        var active = await SeedCustomerWithPoints(db, "C-NEW", 300, DateTimeOffset.UtcNow.AddMonths(-2));

        var result = await new ExpireLoyaltyPointsCommandHandler(db)
            .Handle(new ExpireLoyaltyPointsCommand(InactivityMonths: 12), CancellationToken.None);

        result.Value!.CustomersAffected.Should().Be(0);
        (await db.Customers.FindAsync(active.Id))!.LoyaltyPoints.Should().Be(300);
    }

    [Fact]
    public async Task Expire_OnlyAffectsInactiveWithBalance()
    {
        await using var db = NewContext();
        await SeedCustomerWithPoints(db, "C-A", 100, DateTimeOffset.UtcNow.AddMonths(-24)); // expire
        await SeedCustomerWithPoints(db, "C-B", 200, DateTimeOffset.UtcNow.AddMonths(-1));  // active
        var zero = Customer.Create("C-Z", "Zero", "Balance");                              // no points
        db.Customers.Add(zero);
        await db.SaveChangesAsync();

        var result = await new ExpireLoyaltyPointsCommandHandler(db)
            .Handle(new ExpireLoyaltyPointsCommand(12), CancellationToken.None);

        result.Value!.CustomersAffected.Should().Be(1);
        result.Value.PointsExpired.Should().Be(100);
    }

    [Fact]
    public async Task Expire_InvalidMonths_ReturnsFailure()
    {
        await using var db = NewContext();

        var result = await new ExpireLoyaltyPointsCommandHandler(db)
            .Handle(new ExpireLoyaltyPointsCommand(0), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
