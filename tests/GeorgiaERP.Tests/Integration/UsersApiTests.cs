using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GeorgiaERP.Domain.Identity;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GeorgiaERP.Tests.Integration;

[Collection("Integration")]
public class UsersApiTests : IntegrationTestBase
{
    public UsersApiTests(ErpApiFactory factory) : base(factory) { }

    private Task<HttpClient> AuthenticatedClient()
        => AuthenticatedClient("users_admin", "usersadmin@test.local", "Users", "Admin", "მომხმარებ");

    private async Task<Guid> SeedRole(string code = "test_role")
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existing = db.Roles.FirstOrDefault(r => r.Code == code);
        if (existing is not null) return existing.Id;

        var role = Role.Create(code, $"Test Role {code}", null, "Test role for user tests", false);
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        return role.Id;
    }

    // === Auth Guard ===

    [Fact]
    public async Task Users_WithoutAuth_Returns401()
    {
        var response = await NewClient().GetAsync("/api/v1/users?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Users_Create_WithoutAuth_Returns401()
    {
        var response = await NewClient().PostAsJsonAsync("/api/v1/users", new
        {
            username = "unauth_user",
            email = "unauth@test.local",
            password = "Test@123!",
            firstName = "Test",
            lastName = "User",
            defaultLanguage = "ka",
            roleIds = new List<Guid>()
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // === List Users ===

    [Fact]
    public async Task Users_List_ReturnsPagedResult()
    {
        var client = await AuthenticatedClient();
        var response = await client.GetAsync("/api/v1/users?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(1);
        body.GetProperty("items").GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Users_List_WithSearch_FiltersResults()
    {
        var client = await AuthenticatedClient();

        // Search for the admin user we seeded
        var response = await client.GetAsync("/api/v1/users?page=1&pageSize=10&search=users_admin");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(1);

        var items = body.GetProperty("items");
        items.EnumerateArray().Should().Contain(u =>
            u.GetProperty("username").GetString() == "users_admin");
    }

    [Fact]
    public async Task Users_List_WithActiveFilter_FiltersResults()
    {
        var client = await AuthenticatedClient();

        var response = await client.GetAsync("/api/v1/users?page=1&pageSize=10&isActive=true");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Users_List_Pagination_RespectsPageSize()
    {
        var client = await AuthenticatedClient();

        var response = await client.GetAsync("/api/v1/users?page=1&pageSize=1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").GetArrayLength().Should().BeLessThanOrEqualTo(1);
        body.GetProperty("page").GetInt32().Should().Be(1);
        body.GetProperty("pageSize").GetInt32().Should().Be(1);
    }

    // === Create User ===

    [Fact]
    public async Task Users_Create_WithValidData_ReturnsCreated()
    {
        var client = await AuthenticatedClient();
        var roleId = await SeedRole("create_test_role");
        var username = $"newuser-{Guid.NewGuid():N}"[..20];

        var response = await client.PostAsJsonAsync("/api/v1/users", new
        {
            username,
            email = $"{username}@test.local",
            password = "Strong@Pass1!",
            firstName = "New",
            lastName = "User",
            firstNameKa = "ახალი",
            lastNameKa = "მომხმარებელი",
            phone = "+995555123456",
            defaultLanguage = "ka",
            roleIds = new[] { roleId }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("username").GetString().Should().Be(username);
        body.GetProperty("firstName").GetString().Should().Be("New");
        body.GetProperty("lastName").GetString().Should().Be("User");
        body.GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Users_Create_WithoutRoles_ReturnsCreated()
    {
        var client = await AuthenticatedClient();
        var username = $"norole-{Guid.NewGuid():N}"[..20];

        var response = await client.PostAsJsonAsync("/api/v1/users", new
        {
            username,
            email = $"{username}@test.local",
            password = "Strong@Pass1!",
            firstName = "No",
            lastName = "Roles",
            defaultLanguage = "ka",
            roleIds = new List<Guid>()
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Users_Create_DuplicateUsername_ReturnsFailure()
    {
        var client = await AuthenticatedClient();
        var username = $"dup-{Guid.NewGuid():N}"[..20];

        // Create first user
        var first = await client.PostAsJsonAsync("/api/v1/users", new
        {
            username,
            email = $"{username}@test.local",
            password = "Strong@Pass1!",
            firstName = "First",
            lastName = "User",
            defaultLanguage = "ka",
            roleIds = new List<Guid>()
        });
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        // Try to create duplicate
        var second = await client.PostAsJsonAsync("/api/v1/users", new
        {
            username,
            email = $"other-{username}@test.local",
            password = "Strong@Pass1!",
            firstName = "Second",
            lastName = "User",
            defaultLanguage = "ka",
            roleIds = new List<Guid>()
        });

        // Should fail due to duplicate username
        second.StatusCode.Should().NotBe(HttpStatusCode.Created);
        var body = await second.Content.ReadAsStringAsync();
        body.Should().Contain("already exists");
    }

    [Fact]
    public async Task Users_Create_DuplicateEmail_ReturnsFailure()
    {
        var client = await AuthenticatedClient();
        var email = $"dupemail-{Guid.NewGuid():N}@test.local";

        // Create first user
        var first = await client.PostAsJsonAsync("/api/v1/users", new
        {
            username = $"user1-{Guid.NewGuid():N}"[..20],
            email,
            password = "Strong@Pass1!",
            firstName = "First",
            lastName = "User",
            defaultLanguage = "ka",
            roleIds = new List<Guid>()
        });
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        // Try with same email
        var second = await client.PostAsJsonAsync("/api/v1/users", new
        {
            username = $"user2-{Guid.NewGuid():N}"[..20],
            email,
            password = "Strong@Pass1!",
            firstName = "Second",
            lastName = "User",
            defaultLanguage = "ka",
            roleIds = new List<Guid>()
        });

        second.StatusCode.Should().NotBe(HttpStatusCode.Created);
        var body = await second.Content.ReadAsStringAsync();
        body.Should().Contain("already exists");
    }

    [Fact]
    public async Task Users_Create_WithGeorgianNames_PreservesUnicode()
    {
        var client = await AuthenticatedClient();
        var username = $"georgian-{Guid.NewGuid():N}"[..20];

        var response = await client.PostAsJsonAsync("/api/v1/users", new
        {
            username,
            email = $"{username}@test.local",
            password = "Strong@Pass1!",
            firstName = "Giorgi",
            lastName = "Beridze",
            firstNameKa = "გიორგი",
            lastNameKa = "ბერიძე",
            defaultLanguage = "ka",
            roleIds = new List<Guid>()
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("firstNameKa").GetString().Should().Be("გიორგი");
        body.GetProperty("lastNameKa").GetString().Should().Be("ბერიძე");
    }

    // === Created User Can Login ===

    [Fact]
    public async Task Users_CreatedUser_CanLogin()
    {
        var client = await AuthenticatedClient();
        var username = $"logintest-{Guid.NewGuid():N}"[..20];
        var password = "LoginTest@123!";

        var createResponse = await client.PostAsJsonAsync("/api/v1/users", new
        {
            username,
            email = $"{username}@test.local",
            password,
            firstName = "Login",
            lastName = "Test",
            defaultLanguage = "ka",
            roleIds = new List<Guid>()
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Now try to login as the new user
        var loginClient = NewClient();
        var loginResponse = await loginClient.PostAsJsonAsync("/api/v1/auth/login",
            new { username, password });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        loginBody.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
    }
}
