using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Identity;
using GeorgiaERP.Infrastructure.Persistence;
using GeorgiaERP.Infrastructure.Licensing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GeorgiaERP.Tests.Integration;

[Collection("Integration")]
public class ApiIntegrationTests : IntegrationTestBase
{
    public ApiIntegrationTests(ErpApiFactory factory) : base(factory) { }

    private Task<HttpClient> AuthenticatedClient()
        => AuthenticatedClient("admin", "admin@test.local", "Test", "Admin", "ტესტ");

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var response = await NewClient().GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SecurityHeaders_ArePresent()
    {
        var response = await NewClient().GetAsync("/health");
        response.Headers.Should().Contain(h => h.Key == "X-Content-Type-Options");
        response.Headers.Should().Contain(h => h.Key == "X-Frame-Options");
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var client = await AuthenticatedClient();
        client.DefaultRequestHeaders.Authorization.Should().NotBeNull();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var response = await NewClient().PostAsJsonAsync("/api/v1/auth/login",
            new { username = "nobody", password = "wrong" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var response = await NewClient().GetAsync("/api/v1/products?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Products_Authenticated_Returns200()
    {
        var client = await AuthenticatedClient();
        var response = await client.GetAsync("/api/v1/products?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task License_Status_ReturnsResult()
    {
        var response = await NewClient().GetAsync("/api/v1/license/status");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task License_Activate_WithValidData_Succeeds()
    {
        var key = HmacLicenseKeyValidator.CreateKey(
            "TEST-LICENSE-SIGNING-KEY-WITH-AT-LEAST-THIRTY-TWO-CHARS",
            "Test LLC", DateTimeOffset.UtcNow.AddYears(1), 5, 1);
        var response = await NewClient().PostAsJsonAsync("/api/v1/license/activate",
            new { licenseKey = key, companyName = "Test LLC", contactEmail = "test@test.ge" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("companyName").GetString().Should().Be("Test LLC");
    }

    [Fact]
    public async Task License_Activate_WithEmptyKey_Returns400()
    {
        var response = await NewClient().PostAsJsonAsync("/api/v1/license/activate",
            new { licenseKey = "", companyName = "Test" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PriceLists_CreateAndList()
    {
        var client = await AuthenticatedClient();
        var code = $"PL-{Guid.NewGuid():N}"[..15];

        var createResponse = await client.PostAsJsonAsync("/api/v1/pricing/price-lists",
            new { code, name = "Test Prices", priceType = "Retail", validFrom = DateTimeOffset.UtcNow, priority = 1 });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var listResponse = await client.GetAsync("/api/v1/pricing/price-lists?page=1&pageSize=10");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        list.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Promotions_Create_ReturnsCreated()
    {
        var client = await AuthenticatedClient();
        var code = $"PR-{Guid.NewGuid():N}"[..15];

        var response = await client.PostAsJsonAsync("/api/v1/pricing/promotions",
            new { code, name = "Test Promo", promotionType = "Percentage", discountValue = 10m,
                  validFrom = DateTimeOffset.UtcNow });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Inventory_WithoutAuth_Returns401()
    {
        var response = await NewClient().GetAsync("/api/v1/inventory/stock-levels?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AuthenticatedUser_WithoutPermission_Returns403()
    {
        var username = $"viewer-{Guid.NewGuid():N}";
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();
            db.Users.Add(User.Create(username, $"{username}@test.local",
                passwordService.HashPassword("Valid@123!"), "Read", "Only"));
            await db.SaveChangesAsync();
        }

        var client = NewClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { username, password = "Valid@123!" });
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", body.GetProperty("accessToken").GetString());

        var response = await client.GetAsync("/api/v1/products?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Login_IsLocked_AfterFiveFailedAttempts()
    {
        var username = $"lock-{Guid.NewGuid():N}";
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();
            db.Users.Add(User.Create(username, $"{username}@test.local",
                passwordService.HashPassword("Valid@123!"), "Lock", "Test"));
            await db.SaveChangesAsync();
        }

        var client = NewClient();
        for (var i = 0; i < 5; i++)
        {
            var failed = await client.PostAsJsonAsync("/api/v1/auth/login",
                new { username, password = "wrong" });
            failed.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        var correct = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { username, password = "Valid@123!" });
        correct.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- Product CRUD Lifecycle ---

    private async Task<Guid> SeedCategory()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var category = db.Categories.FirstOrDefault();
        if (category is not null) return category.Id;

        category = GeorgiaERP.Domain.Products.Category.Create($"CAT-{Guid.NewGuid():N}"[..10], "Test Category");
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return category.Id;
    }

    [Fact]
    public async Task Products_CreateAndGetById()
    {
        var client = await AuthenticatedClient();
        var categoryId = await SeedCategory();
        var sku = $"SKU-{Guid.NewGuid():N}"[..15];

        var createResponse = await client.PostAsJsonAsync("/api/v1/products",
            new
            {
                sku,
                name = "Integration Test Product",
                nameKa = "ტესტ პროდუქტი",
                categoryId,
                unitOfMeasure = "PCS",
                vatApplicable = true,
                isSerialized = false,
                isBatchTracked = false,
                hasExpiry = false
            });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var productId = created.GetProperty("id").GetString()!;

        var getResponse = await client.GetAsync($"/api/v1/products/{productId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var product = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        product.GetProperty("sku").GetString().Should().Be(sku);
        product.GetProperty("name").GetString().Should().Be("Integration Test Product");
    }

    [Fact]
    public async Task Products_Update_ReturnsOk()
    {
        var client = await AuthenticatedClient();
        var categoryId = await SeedCategory();
        var sku = $"SKU-{Guid.NewGuid():N}"[..15];

        var createResponse = await client.PostAsJsonAsync("/api/v1/products",
            new
            {
                sku,
                name = "Before Update",
                categoryId,
                unitOfMeasure = "PCS",
                vatApplicable = true,
                isSerialized = false,
                isBatchTracked = false,
                hasExpiry = false
            });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var productId = created.GetProperty("id").GetString()!;

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/products/{productId}",
            new { name = "After Update", weightKg = 2.5m });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await client.GetAsync($"/api/v1/products/{productId}");
        var product = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        product.GetProperty("name").GetString().Should().Be("After Update");
    }

    [Fact]
    public async Task Products_Delete_ReturnsNoContent()
    {
        var client = await AuthenticatedClient();
        var categoryId = await SeedCategory();
        var sku = $"SKU-{Guid.NewGuid():N}"[..15];

        var createResponse = await client.PostAsJsonAsync("/api/v1/products",
            new
            {
                sku,
                name = "To Be Deleted",
                categoryId,
                unitOfMeasure = "PCS",
                vatApplicable = true,
                isSerialized = false,
                isBatchTracked = false,
                hasExpiry = false
            });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var productId = created.GetProperty("id").GetString()!;

        var deleteResponse = await client.DeleteAsync($"/api/v1/products/{productId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Product should still be accessible (soft delete) but inactive
        var getResponse = await client.GetAsync($"/api/v1/products/{productId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var product = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        product.GetProperty("isActive").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Products_Delete_NotFound_Returns404()
    {
        var client = await AuthenticatedClient();

        var response = await client.DeleteAsync($"/api/v1/products/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Products_Update_NotFound_Returns404()
    {
        var client = await AuthenticatedClient();

        var response = await client.PutAsJsonAsync($"/api/v1/products/{Guid.NewGuid()}",
            new { name = "Nonexistent" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
