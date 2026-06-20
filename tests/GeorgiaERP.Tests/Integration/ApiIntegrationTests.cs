using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Identity;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GeorgiaERP.Tests.Integration;

public class ApiIntegrationTests : IClassFixture<ErpApiFactory>
{
    private readonly ErpApiFactory _factory;

    public ApiIntegrationTests(ErpApiFactory factory) => _factory = factory;

    private HttpClient NewClient() => _factory.CreateClient();

    private async Task<HttpClient> AuthenticatedClient()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();

        if (!db.Users.Any(u => u.Username == "admin"))
        {
            var role = Role.Create("super_admin", "Super Admin", "სუპერ ადმინი", "Full access", true);
            db.Roles.Add(role);

            var user = User.Create("admin", "admin@test.local",
                passwordService.HashPassword("Admin@123!"),
                "Test", "Admin", "ტესტ", "ადმინი", "ka");
            db.Users.Add(user);
            db.UserRoles.Add(UserRole.Create(user.Id, role.Id));
            await db.SaveChangesAsync();
        }

        var client = NewClient();
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { username = "admin", password = "Admin@123!" });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

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
        var response = await NewClient().PostAsJsonAsync("/api/v1/license/activate",
            new { licenseKey = $"KEY-{Guid.NewGuid():N}", companyName = "Test LLC", contactEmail = "test@test.ge" });

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
}
