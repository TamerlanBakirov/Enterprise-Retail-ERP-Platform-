using FluentAssertions;
using GeorgiaERP.Application.CRM.Commands;
using GeorgiaERP.Application.CRM.Queries;
using GeorgiaERP.Domain.CRM;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GeorgiaERP.Tests.Application;

public class LoyaltyHistoryQueryTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"loyalty-{Guid.NewGuid()}")
            .Options);

    private static async Task<Guid> SeedCustomer(AppDbContext db)
    {
        var customer = Customer.Create("C-1001", "Nino", "Beridze");
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return customer.Id;
    }

    [Fact]
    public async Task History_AfterEarnAndRedeem_ReturnsLedgerNewestFirst()
    {
        await using var db = NewContext();
        var customerId = await SeedCustomer(db);
        await new EarnLoyaltyPointsCommandHandler(db)
            .Handle(new EarnLoyaltyPointsCommand(customerId, 100, "pos", null, "Purchase"), CancellationToken.None);
        await new RedeemLoyaltyPointsCommandHandler(db)
            .Handle(new RedeemLoyaltyPointsCommand(customerId, 30, "Discount"), CancellationToken.None);

        var result = await new GetLoyaltyHistoryQueryHandler(db)
            .Handle(new GetLoyaltyHistoryQuery(customerId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var page = result.Value!;
        page.TotalCount.Should().Be(2);
        page.Items.Should().HaveCount(2);
        page.Items[0].TransactionType.Should().Be("Redeem"); // newest first
        page.Items[0].BalanceAfter.Should().Be(70);
        page.Items[1].TransactionType.Should().Be("Earn");
        page.Items[1].BalanceAfter.Should().Be(100);
    }

    [Fact]
    public async Task History_UnknownCustomer_ReturnsNotFound()
    {
        await using var db = NewContext();

        var result = await new GetLoyaltyHistoryQueryHandler(db)
            .Handle(new GetLoyaltyHistoryQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task History_IsScopedToTheRequestedCustomer()
    {
        await using var db = NewContext();
        var a = await SeedCustomer(db);
        var other = Customer.Create("C-2002", "Giorgi", "Kapanadze");
        db.Customers.Add(other);
        await db.SaveChangesAsync();
        await new EarnLoyaltyPointsCommandHandler(db)
            .Handle(new EarnLoyaltyPointsCommand(a, 50, null, null, null), CancellationToken.None);
        await new EarnLoyaltyPointsCommandHandler(db)
            .Handle(new EarnLoyaltyPointsCommand(other.Id, 999, null, null, null), CancellationToken.None);

        var result = await new GetLoyaltyHistoryQueryHandler(db)
            .Handle(new GetLoyaltyHistoryQuery(a), CancellationToken.None);

        result.Value!.TotalCount.Should().Be(1);
        result.Value.Items[0].Points.Should().Be(50);
    }

    [Fact]
    public async Task History_Paginates()
    {
        await using var db = NewContext();
        var customerId = await SeedCustomer(db);
        var earn = new EarnLoyaltyPointsCommandHandler(db);
        for (var i = 0; i < 5; i++)
            await earn.Handle(new EarnLoyaltyPointsCommand(customerId, 10, null, null, $"earn {i}"), CancellationToken.None);
        var handler = new GetLoyaltyHistoryQueryHandler(db);

        var p1 = await handler.Handle(new GetLoyaltyHistoryQuery(customerId, Page: 1, PageSize: 2), CancellationToken.None);
        var p2 = await handler.Handle(new GetLoyaltyHistoryQuery(customerId, Page: 2, PageSize: 2), CancellationToken.None);

        p1.Value!.TotalCount.Should().Be(5);
        p1.Value.Items.Should().HaveCount(2);
        p1.Value.PageSize.Should().Be(2);
        // Page 2 advances past page 1 — no overlap.
        p2.Value!.Items.Should().HaveCount(2);
        p2.Value.Items.Select(i => i.Id).Should().NotIntersectWith(p1.Value.Items.Select(i => i.Id));
    }
}
