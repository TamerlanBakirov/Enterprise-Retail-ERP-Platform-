using FluentAssertions;
using GeorgiaERP.Application.Pricing.Commands;
using GeorgiaERP.Domain.Pricing;
using GeorgiaERP.Domain.Products;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GeorgiaERP.Tests.Application;

public class PricingCommandHandlerTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"pricing-{Guid.NewGuid()}")
            .Options);

    // === CreatePriceList ===

    [Fact]
    public async Task CreatePriceList_Valid_ReturnsSuccess()
    {
        await using var db = NewContext();
        var handler = new CreatePriceListCommandHandler(db);

        var result = await handler.Handle(new CreatePriceListCommand(
            "PL-001", "Retail Prices", "საცალო ფასები",
            "Retail", null, DateTimeOffset.UtcNow, null, 1),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("PL-001");
        (await db.PriceLists.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreatePriceList_DuplicateCode_ReturnsFailure()
    {
        await using var db = NewContext();
        var existing = PriceList.Create("PL-DUP", "Existing", PriceType.Retail, DateTimeOffset.UtcNow);
        db.PriceLists.Add(existing);
        await db.SaveChangesAsync();

        var handler = new CreatePriceListCommandHandler(db);
        var result = await handler.Handle(new CreatePriceListCommand(
            "PL-DUP", "Duplicate", null, "Retail", null, DateTimeOffset.UtcNow, null, 1),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task CreatePriceList_InvalidPriceType_ReturnsFailure()
    {
        await using var db = NewContext();
        var handler = new CreatePriceListCommandHandler(db);

        var result = await handler.Handle(new CreatePriceListCommand(
            "PL-BAD", "Bad Type", null, "NotAType", null, DateTimeOffset.UtcNow, null, 1),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid price type");
    }

    // === SetPrice ===

    [Fact]
    public async Task SetPrice_Valid_CreatesItem()
    {
        await using var db = NewContext();
        var priceList = PriceList.Create("PL-SET", "List", PriceType.Retail, DateTimeOffset.UtcNow);
        db.PriceLists.Add(priceList);
        var cat = Category.Create("CAT-PR", "Category");
        db.Categories.Add(cat);
        var product = Product.Create("SKU-PR", "Product", cat.Id, "Piece");
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var handler = new SetPriceCommandHandler(db);
        var result = await handler.Handle(new SetPriceCommand(
            priceList.Id, product.Id, 25.99m, 1m, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await db.PriceListItems.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task SetPrice_InvalidPriceList_ReturnsFailure()
    {
        await using var db = NewContext();
        var handler = new SetPriceCommandHandler(db);

        var result = await handler.Handle(new SetPriceCommand(
            Guid.NewGuid(), Guid.NewGuid(), 10m, 1m, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    // === CreatePromotion ===

    [Fact]
    public async Task CreatePromotion_Valid_ReturnsSuccess()
    {
        await using var db = NewContext();
        var handler = new CreatePromotionCommandHandler(db);

        var result = await handler.Handle(new CreatePromotionCommand(
            "PROMO-001", "Summer Sale", "ზაფხულის ფასდაკლება",
            "Percentage", 15m, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30), null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await db.Promotions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreatePromotion_DuplicateCode_ReturnsFailure()
    {
        await using var db = NewContext();
        var existing = Promotion.Create("PROMO-DUP", "Existing", PromotionType.Percentage,
            DateTimeOffset.UtcNow, 10m);
        db.Promotions.Add(existing);
        await db.SaveChangesAsync();

        var handler = new CreatePromotionCommandHandler(db);
        var result = await handler.Handle(new CreatePromotionCommand(
            "PROMO-DUP", "Duplicate", null, "Percentage", 20m, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(7), null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }
}
