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
public class CustomerApiTests
{
    private readonly ErpApiFactory _factory;

    public CustomerApiTests(ErpApiFactory factory) => _factory = factory;

    private HttpClient NewClient() => _factory.CreateClient();

    private async Task<HttpClient> AuthenticatedClient()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();

        if (!db.Users.Any(u => u.Username == "crm_admin"))
        {
            var role = db.Roles.FirstOrDefault(r => r.Code == "super_admin");
            if (role is null)
            {
                role = Role.Create("super_admin", "Super Admin", "სუპერ ადმინი", "Full access", true);
                db.Roles.Add(role);
            }

            var user = User.Create("crm_admin", "crmadmin@test.local",
                passwordService.HashPassword("Admin@123!"),
                "CRM", "Admin", "ტესტ", "ადმინი", "ka");
            db.Users.Add(user);
            db.UserRoles.Add(UserRole.Create(user.Id, role.Id));
            await db.SaveChangesAsync();
        }

        var client = NewClient();
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { username = "crm_admin", password = "Admin@123!" });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    // === Customer Tests ===

    [Fact]
    public async Task Customers_WithoutAuth_Returns401()
    {
        var response = await NewClient().GetAsync("/api/v1/customers");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Customers_CreateAndList()
    {
        var client = await AuthenticatedClient();
        var unique = Guid.NewGuid().ToString("N")[..8];

        var createResponse = await client.PostAsJsonAsync("/api/v1/customers", new
        {
            firstName = $"John{unique}",
            lastName = $"Doe{unique}",
            phone = $"+995555{unique[..6]}",
            email = $"john{unique}@test.local",
            consentSms = true,
            consentEmail = false
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        created.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
        created.GetProperty("customerNumber").GetString().Should().NotBeNullOrEmpty();

        var listResponse = await client.GetAsync("/api/v1/customers");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        list.GetProperty("items").GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Customers_CreateWithKaNames()
    {
        var client = await AuthenticatedClient();
        var unique = Guid.NewGuid().ToString("N")[..8];

        var createResponse = await client.PostAsJsonAsync("/api/v1/customers", new
        {
            firstName = $"Giorgi{unique}",
            lastName = $"Beridze{unique}",
            firstNameKa = "გიორგი",
            lastNameKa = "ბერიძე",
            phone = $"+995577{unique[..6]}",
            consentSms = false,
            consentEmail = false
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        created.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
        created.GetProperty("customerNumber").GetString().Should().StartWith("C-");
    }

    [Fact]
    public async Task Customers_LoyaltyEarnPoints()
    {
        var client = await AuthenticatedClient();
        var unique = Guid.NewGuid().ToString("N")[..8];

        // Create customer
        var createResponse = await client.PostAsJsonAsync("/api/v1/customers", new
        {
            firstName = $"Earn{unique}",
            lastName = $"Test{unique}",
            phone = $"+995599{unique[..6]}",
            consentSms = false,
            consentEmail = false
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var customerId = created.GetProperty("id").GetString()!;

        // Earn points
        var earnResponse = await client.PostAsJsonAsync($"/api/v1/customers/{customerId}/loyalty/earn",
            new { points = 100, description = "Welcome bonus" });

        earnResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var earnBody = await earnResponse.Content.ReadFromJsonAsync<JsonElement>();
        earnBody.GetProperty("balance").GetInt32().Should().Be(100);
    }

    [Fact]
    public async Task Customers_LoyaltyRedeemPoints()
    {
        var client = await AuthenticatedClient();
        var unique = Guid.NewGuid().ToString("N")[..8];

        // Create customer
        var createResponse = await client.PostAsJsonAsync("/api/v1/customers", new
        {
            firstName = $"Redeem{unique}",
            lastName = $"Test{unique}",
            phone = $"+995598{unique[..6]}",
            consentSms = false,
            consentEmail = false
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var customerId = created.GetProperty("id").GetString()!;

        // Earn 100 points
        var earnResponse = await client.PostAsJsonAsync($"/api/v1/customers/{customerId}/loyalty/earn",
            new { points = 100, description = "Initial load" });
        earnResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Redeem 50 points
        var redeemResponse = await client.PostAsJsonAsync($"/api/v1/customers/{customerId}/loyalty/redeem",
            new { points = 50, description = "Partial redeem" });

        redeemResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var redeemBody = await redeemResponse.Content.ReadFromJsonAsync<JsonElement>();
        redeemBody.GetProperty("balance").GetInt32().Should().Be(50);
    }

    [Fact]
    public async Task Customers_RedeemMoreThanBalance_Fails()
    {
        var client = await AuthenticatedClient();
        var unique = Guid.NewGuid().ToString("N")[..8];

        // Create customer
        var createResponse = await client.PostAsJsonAsync("/api/v1/customers", new
        {
            firstName = $"OverRedeem{unique}",
            lastName = $"Test{unique}",
            phone = $"+995597{unique[..6]}",
            consentSms = false,
            consentEmail = false
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var customerId = created.GetProperty("id").GetString()!;

        // Earn 10 points
        var earnResponse = await client.PostAsJsonAsync($"/api/v1/customers/{customerId}/loyalty/earn",
            new { points = 10, description = "Small load" });
        earnResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Try to redeem 50 points (more than balance)
        var redeemResponse = await client.PostAsJsonAsync($"/api/v1/customers/{customerId}/loyalty/redeem",
            new { points = 50, description = "Too many" });

        redeemResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Customers_SearchByName()
    {
        var client = await AuthenticatedClient();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var uniqueName = $"SearchTest{unique}";

        // Create customer with unique name
        var createResponse = await client.PostAsJsonAsync("/api/v1/customers", new
        {
            firstName = uniqueName,
            lastName = $"Surname{unique}",
            phone = $"+995596{unique[..6]}",
            consentSms = false,
            consentEmail = false
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Search by the unique name
        var searchResponse = await client.GetAsync($"/api/v1/customers?search={uniqueName}");

        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var searchBody = await searchResponse.Content.ReadFromJsonAsync<JsonElement>();
        searchBody.GetProperty("items").GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
    }
}
