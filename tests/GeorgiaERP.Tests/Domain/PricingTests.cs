using FluentAssertions;
using GeorgiaERP.Domain.Pricing;
using Xunit;

namespace GeorgiaERP.Tests.Domain;

public class PricingTests
{
    // === PriceList ===

    [Fact]
    public void CreatePriceList_SetsDefaultValues()
    {
        var validFrom = DateTimeOffset.UtcNow;
        var priceList = PriceList.Create("PL-RETAIL", "Retail Prices", PriceType.Retail, validFrom, "საცალო ფასები");

        priceList.Code.Should().Be("PL-RETAIL");
        priceList.Name.Should().Be("Retail Prices");
        priceList.NameKa.Should().Be("საცალო ფასები");
        priceList.PriceType.Should().Be(PriceType.Retail);
        priceList.ValidFrom.Should().Be(validFrom);
        priceList.ValidTo.Should().BeNull();
        priceList.IsActive.Should().BeTrue();
        priceList.Priority.Should().Be(0);
        priceList.Currency.Should().Be("GEL");
        priceList.StoreId.Should().BeNull();
    }

    [Theory]
    [InlineData(PriceType.Retail)]
    [InlineData(PriceType.Wholesale)]
    [InlineData(PriceType.Employee)]
    [InlineData(PriceType.Cost)]
    public void PriceList_AllTypes_CanBeCreated(PriceType type)
    {
        var priceList = PriceList.Create("PL-001", "Test", type, DateTimeOffset.UtcNow);

        priceList.PriceType.Should().Be(type);
    }

    // === PriceListItem ===

    [Fact]
    public void CreatePriceListItem_SetsDefaultValues()
    {
        var priceListId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var item = PriceListItem.Create(priceListId, productId, 29.99m);

        item.PriceListId.Should().Be(priceListId);
        item.ProductId.Should().Be(productId);
        item.Price.Should().Be(29.99m);
        item.MinQty.Should().Be(1);
        item.VariantId.Should().BeNull();
    }

    [Fact]
    public void CreatePriceListItem_WithMinQty_SetsMinQty()
    {
        var item = PriceListItem.Create(Guid.NewGuid(), Guid.NewGuid(), 19.99m, minQty: 10);

        item.MinQty.Should().Be(10);
    }

    [Fact]
    public void CreatePriceListItem_WithVariant_SetsVariantId()
    {
        var variantId = Guid.NewGuid();
        var item = PriceListItem.Create(Guid.NewGuid(), Guid.NewGuid(), 24.99m, variantId: variantId);

        item.VariantId.Should().Be(variantId);
    }

    // === Promotion ===

    [Fact]
    public void CreatePromotion_SetsDefaultValues()
    {
        var validFrom = DateTimeOffset.UtcNow;
        var promo = Promotion.Create("PROMO-001", "Summer Sale", PromotionType.Percentage, validFrom,
            discountValue: 15m, nameKa: "ზაფხულის ფასდაკლება");

        promo.Code.Should().Be("PROMO-001");
        promo.Name.Should().Be("Summer Sale");
        promo.NameKa.Should().Be("ზაფხულის ფასდაკლება");
        promo.PromotionType.Should().Be(PromotionType.Percentage);
        promo.DiscountValue.Should().Be(15m);
        promo.ValidFrom.Should().Be(validFrom);
        promo.ValidTo.Should().BeNull();
        promo.IsActive.Should().BeTrue();
        promo.CurrentUses.Should().Be(0);
        promo.MaxUses.Should().BeNull();
        promo.Conditions.Should().BeNull();
        promo.StoreIds.Should().BeNull();
    }

    [Theory]
    [InlineData(PromotionType.Percentage)]
    [InlineData(PromotionType.Fixed)]
    [InlineData(PromotionType.BuyOneGetOne)]
    [InlineData(PromotionType.Bundle)]
    public void Promotion_AllTypes_CanBeCreated(PromotionType type)
    {
        var promo = Promotion.Create("PR-001", "Test", type, DateTimeOffset.UtcNow);

        promo.PromotionType.Should().Be(type);
    }

    [Fact]
    public void CreatePromotion_WithNoDiscount_AcceptsNullValue()
    {
        var promo = Promotion.Create("BOGO-001", "Buy One Get One", PromotionType.BuyOneGetOne,
            DateTimeOffset.UtcNow);

        promo.DiscountValue.Should().BeNull();
    }
}
