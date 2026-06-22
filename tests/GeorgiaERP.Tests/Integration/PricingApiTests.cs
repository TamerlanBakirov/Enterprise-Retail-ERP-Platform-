using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace GeorgiaERP.Tests.Integration;

[Collection("Integration")]
public class PricingApiTests : IntegrationTestBase
{
    public PricingApiTests(ErpApiFactory factory) : base(factory) { }

    private Task<HttpClient> AuthenticatedClient()
        => AuthenticatedClient("pricing_admin", "pricingadmin@test.local", "Pricing");

    // ===== Auth guard tests =====

    [Fact]
    public async Task GetPriceLists_WithoutAuth_Returns401()
    {
        var response = await NewClient().GetAsync("/api/v1/pricing/price-lists");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPromotions_WithoutAuth_Returns401()
    {
        var response = await NewClient().GetAsync("/api/v1/pricing/promotions");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ===== Price Lists =====

    [Fact]
    public async Task GetPriceLists_Empty_ReturnsOk()
    {
        var client = await AuthenticatedClient();
        var response = await client.GetAsync("/api/v1/pricing/price-lists");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").GetArrayLength().Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task CreatePriceList_Valid_ReturnsCreated()
    {
        var client = await AuthenticatedClient();
        var code = $"PL-{Guid.NewGuid():N}"[..12];

        var response = await client.PostAsJsonAsync("/api/v1/pricing/price-lists", new
        {
            Code = code,
            Name = "Test Price List",
            NameKa = "ტესტ ფასების სია",
            PriceType = "Retail",
            ValidFrom = DateTimeOffset.UtcNow.ToString("o"),
            Priority = 1
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("code").GetString().Should().Be(code);
        body.GetProperty("name").GetString().Should().Be("Test Price List");
        body.GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task CreatePriceList_DuplicateCode_ReturnsBadRequest()
    {
        var client = await AuthenticatedClient();
        var code = $"PL-{Guid.NewGuid():N}"[..12];

        // Create first
        await client.PostAsJsonAsync("/api/v1/pricing/price-lists", new
        {
            Code = code,
            Name = "First List",
            PriceType = "Retail",
            ValidFrom = DateTimeOffset.UtcNow.ToString("o"),
            Priority = 1
        });

        // Try duplicate
        var response = await client.PostAsJsonAsync("/api/v1/pricing/price-lists", new
        {
            Code = code,
            Name = "Duplicate List",
            PriceType = "Retail",
            ValidFrom = DateTimeOffset.UtcNow.ToString("o"),
            Priority = 1
        });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreatePriceList_InvalidType_ReturnsBadRequest()
    {
        var client = await AuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/v1/pricing/price-lists", new
        {
            Code = $"PL-{Guid.NewGuid():N}"[..12],
            Name = "Bad Type List",
            PriceType = "InvalidType",
            ValidFrom = DateTimeOffset.UtcNow.ToString("o"),
            Priority = 1
        });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetPriceListItems_NonExistentList_ReturnsOk()
    {
        var client = await AuthenticatedClient();
        var response = await client.GetAsync($"/api/v1/pricing/price-lists/{Guid.NewGuid()}/items");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    // ===== Promotions =====

    [Fact]
    public async Task GetPromotions_Empty_ReturnsOk()
    {
        var client = await AuthenticatedClient();
        var response = await client.GetAsync("/api/v1/pricing/promotions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").GetArrayLength().Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task CreatePromotion_Valid_ReturnsCreated()
    {
        var client = await AuthenticatedClient();
        var code = $"PROMO-{Guid.NewGuid():N}"[..14];

        var response = await client.PostAsJsonAsync("/api/v1/pricing/promotions", new
        {
            Code = code,
            Name = "Test Promotion",
            NameKa = "ტესტ აქცია",
            PromotionType = "Percentage",
            DiscountValue = 10.0,
            ValidFrom = DateTimeOffset.UtcNow.ToString("o"),
            ValidTo = DateTimeOffset.UtcNow.AddDays(30).ToString("o"),
            MaxUses = 100
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("code").GetString().Should().Be(code);
        body.GetProperty("name").GetString().Should().Be("Test Promotion");
        body.GetProperty("isActive").GetBoolean().Should().BeTrue();
        body.GetProperty("currentUses").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task CreatePromotion_DuplicateCode_ReturnsBadRequest()
    {
        var client = await AuthenticatedClient();
        var code = $"PROMO-{Guid.NewGuid():N}"[..14];

        // Create first
        await client.PostAsJsonAsync("/api/v1/pricing/promotions", new
        {
            Code = code,
            Name = "First Promo",
            PromotionType = "Percentage",
            DiscountValue = 5.0,
            ValidFrom = DateTimeOffset.UtcNow.ToString("o"),
        });

        // Try duplicate
        var response = await client.PostAsJsonAsync("/api/v1/pricing/promotions", new
        {
            Code = code,
            Name = "Duplicate Promo",
            PromotionType = "Percentage",
            DiscountValue = 10.0,
            ValidFrom = DateTimeOffset.UtcNow.ToString("o"),
        });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetPromotions_WithActiveFilter_ReturnsOk()
    {
        var client = await AuthenticatedClient();
        var response = await client.GetAsync("/api/v1/pricing/promotions?isActive=true&page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPriceLists_WithPagination_ReturnsOk()
    {
        var client = await AuthenticatedClient();
        var response = await client.GetAsync("/api/v1/pricing/price-lists?page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("page").GetInt32().Should().Be(1);
        body.GetProperty("pageSize").GetInt32().Should().Be(5);
    }
}
