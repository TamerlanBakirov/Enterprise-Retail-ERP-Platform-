using FluentAssertions;
using GeorgiaERP.Application.Pricing.Commands;
using GeorgiaERP.Domain.Pricing;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GeorgiaERP.Tests.Application;

public class SetPromotionStatusHandlerTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"promo-status-{Guid.NewGuid()}")
            .Options);

    private static async Task<Promotion> SeedPromotion(AppDbContext db)
    {
        var p = Promotion.Create("PROMO-1", "Summer Sale", PromotionType.Percentage,
            DateTimeOffset.UtcNow, 15m);
        db.Promotions.Add(p);
        await db.SaveChangesAsync();
        return p;
    }

    [Fact]
    public async Task Deactivate_SetsInactive()
    {
        await using var db = NewContext();
        var p = await SeedPromotion(db);

        var result = await new SetPromotionStatusCommandHandler(db)
            .Handle(new SetPromotionStatusCommand(p.Id, IsActive: false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsActive.Should().BeFalse();
        (await db.Promotions.FindAsync(p.Id))!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Activate_SetsActive()
    {
        await using var db = NewContext();
        var p = await SeedPromotion(db);
        p.Deactivate();
        await db.SaveChangesAsync();

        var result = await new SetPromotionStatusCommandHandler(db)
            .Handle(new SetPromotionStatusCommand(p.Id, IsActive: true), CancellationToken.None);

        result.Value!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task SetStatus_Unknown_ReturnsNotFound()
    {
        await using var db = NewContext();

        var result = await new SetPromotionStatusCommandHandler(db)
            .Handle(new SetPromotionStatusCommand(Guid.NewGuid(), IsActive: false), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }
}
