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

[Collection("Integration")]
public class AnalyticsTests
{
    private readonly ErpApiFactory _factory;

    public AnalyticsTests(ErpApiFactory factory) => _factory = factory;

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
    public async Task Dashboard_Authenticated_Returns200()
    {
        var client = await AuthenticatedClient();
        var response = await client.GetAsync("/api/v1/analytics/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalRevenue").GetDecimal().Should().BeGreaterThanOrEqualTo(0);
        body.GetProperty("totalOrders").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        body.GetProperty("totalProducts").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        body.GetProperty("activeCustomers").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        body.GetProperty("topSellingProducts").GetArrayLength().Should().BeGreaterThanOrEqualTo(0);
        body.GetProperty("revenueTrend").GetArrayLength().Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Dashboard_WithoutAuth_Returns401()
    {
        var response = await NewClient().GetAsync("/api/v1/analytics/dashboard");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RevenueTrend_Authenticated_Returns200()
    {
        var client = await AuthenticatedClient();
        var response = await client.GetAsync("/api/v1/analytics/revenue-trend?days=7");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task RevenueTrend_WithoutAuth_Returns401()
    {
        var response = await NewClient().GetAsync("/api/v1/analytics/revenue-trend");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SalesByCategory_Authenticated_Returns200()
    {
        var client = await AuthenticatedClient();
        var response = await client.GetAsync("/api/v1/analytics/sales-by-category");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task SalesByCategory_WithoutAuth_Returns401()
    {
        var response = await NewClient().GetAsync("/api/v1/analytics/sales-by-category");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task StockSummary_Authenticated_Returns200()
    {
        var client = await AuthenticatedClient();
        var response = await client.GetAsync("/api/v1/analytics/stock-summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalItems").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        body.GetProperty("lowStockItems").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        body.GetProperty("outOfStockItems").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        body.GetProperty("totalValue").GetDecimal().Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task StockSummary_WithoutAuth_Returns401()
    {
        var response = await NewClient().GetAsync("/api/v1/analytics/stock-summary");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
