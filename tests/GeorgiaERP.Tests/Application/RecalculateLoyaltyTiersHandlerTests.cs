using FluentAssertions;
using GeorgiaERP.Application.CRM.Commands;
using GeorgiaERP.Domain.CRM;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GeorgiaERP.Tests.Application;

public class RecalculateLoyaltyTiersHandlerTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"tiers-{Guid.NewGuid()}")
            .Options);

    private static Customer CustomerWithSpend(string code, decimal spend)
    {
        var c = Customer.Create(code, "Test", "Customer");
        if (spend > 0) c.RecordVisit(spend);
        return c;
    }

    [Theory]
    [InlineData(0, "Bronze")]
    [InlineData(999.99, "Bronze")]
    [InlineData(1000, "Silver")]
    [InlineData(4999.99, "Silver")]
    [InlineData(5000, "Gold")]
    [InlineData(12000, "Gold")]
    public void Policy_MapsSpendToTier(decimal spend, string expected)
    {
        LoyaltyTierPolicy.ForSpend(spend).Should().Be(expected);
    }

    [Fact]
    public async Task Recalculate_AssignsTiersBySpend()
    {
        await using var db = NewContext();
        db.Customers.Add(CustomerWithSpend("C-BRONZE", 200m));
        db.Customers.Add(CustomerWithSpend("C-SILVER", 1500m));
        db.Customers.Add(CustomerWithSpend("C-GOLD", 8000m));
        await db.SaveChangesAsync();

        var result = await new RecalculateLoyaltyTiersCommandHandler(db)
            .Handle(new RecalculateLoyaltyTiersCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CustomersEvaluated.Should().Be(3);
        result.Value.TiersChanged.Should().Be(3);

        (await db.Customers.SingleAsync(c => c.CustomerNumber == "C-SILVER")).LoyaltyTier.Should().Be("Silver");
        (await db.Customers.SingleAsync(c => c.CustomerNumber == "C-GOLD")).LoyaltyTier.Should().Be("Gold");
    }

    [Fact]
    public async Task Recalculate_IsIdempotent()
    {
        await using var db = NewContext();
        db.Customers.Add(CustomerWithSpend("C-1", 6000m));
        await db.SaveChangesAsync();
        var handler = new RecalculateLoyaltyTiersCommandHandler(db);
        await handler.Handle(new RecalculateLoyaltyTiersCommand(), CancellationToken.None);

        var second = await handler.Handle(new RecalculateLoyaltyTiersCommand(), CancellationToken.None);

        second.Value!.TiersChanged.Should().Be(0); // already at Gold, no change
    }

    [Fact]
    public async Task Recalculate_SkipsInactiveCustomers()
    {
        await using var db = NewContext();
        var inactive = CustomerWithSpend("C-OFF", 9000m);
        inactive.Deactivate();
        db.Customers.Add(inactive);
        await db.SaveChangesAsync();

        var result = await new RecalculateLoyaltyTiersCommandHandler(db)
            .Handle(new RecalculateLoyaltyTiersCommand(), CancellationToken.None);

        result.Value!.CustomersEvaluated.Should().Be(0);
        (await db.Customers.SingleAsync(c => c.CustomerNumber == "C-OFF")).LoyaltyTier.Should().BeNull();
    }
}
