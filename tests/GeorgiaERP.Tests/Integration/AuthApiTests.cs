using System.Net;
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
public class AuthApiTests : IntegrationTestBase
{
    public AuthApiTests(ErpApiFactory factory) : base(factory) { }

    private async Task SeedUser(string username = "auth_test", string password = "Admin@123!")
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();

        if (db.Users.Any(u => u.Username == username)) return;

        var role = db.Roles.FirstOrDefault(r => r.Code == "super_admin");
        if (role is null)
        {
            role = Role.Create("super_admin", "Super Admin", "სუპერ ადმინი", "Full access", true);
            db.Roles.Add(role);
        }

        var user = User.Create(username, $"{username}@test.local",
            passwordService.HashPassword(password),
            "Auth", "Test", "ავტორიზ", "ტესტი", "ka");
        db.Users.Add(user);
        db.UserRoles.Add(UserRole.Create(user.Id, role.Id));
        await db.SaveChangesAsync();
    }

    // === Login ===

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokens()
    {
        await SeedUser("auth_login_ok");
        var client = NewClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            username = "auth_login_ok",
            password = "Admin@123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_InvalidPassword_Returns401()
    {
        await SeedUser("auth_bad_pw");
        var client = NewClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            username = "auth_bad_pw",
            password = "WrongPassword123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_NonExistentUser_Returns401()
    {
        var client = NewClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            username = "nobody_exists_here",
            password = "Admin@123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // === Refresh Token ===

    [Fact]
    public async Task RefreshToken_ValidToken_ReturnsNewTokens()
    {
        await SeedUser("auth_refresh");
        var client = NewClient();

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            username = "auth_refresh",
            password = "Admin@123!"
        });
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var refreshToken = loginBody.GetProperty("refreshToken").GetString()!;

        var refreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken
        });

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await refreshResponse.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RefreshToken_InvalidToken_Returns401()
    {
        var client = NewClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = "totally-bogus-token"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // === Logout ===

    [Fact]
    public async Task Logout_Authenticated_ReturnsOk()
    {
        var client = await AuthenticatedClient("auth_logout", "authlogout@test.local");

        var loginResponse = await NewClient().PostAsJsonAsync("/api/v1/auth/login", new
        {
            username = "auth_logout",
            password = "Admin@123!"
        });
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var refreshToken = loginBody.GetProperty("refreshToken").GetString()!;

        var response = await client.PostAsJsonAsync("/api/v1/auth/logout", new
        {
            refreshToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_Unauthenticated_Returns401()
    {
        var client = NewClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/logout", new
        {
            refreshToken = "some-token"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // === Me ===

    [Fact]
    public async Task GetMe_Authenticated_ReturnsUserInfo()
    {
        var client = await AuthenticatedClient("auth_me", "authme@test.local");

        var response = await client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("username").GetString().Should().Be("auth_me");
    }

    [Fact]
    public async Task GetMe_Unauthenticated_Returns401()
    {
        var response = await NewClient().GetAsync("/api/v1/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // === 2FA Setup ===

    [Fact]
    public async Task TwoFactorSetup_Authenticated_ReturnsOk()
    {
        var client = await AuthenticatedClient("auth_2fa_setup", "auth2fa@test.local");

        var response = await client.PostAsync("/api/v1/auth/2fa/setup", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TwoFactorSetup_Unauthenticated_Returns401()
    {
        var response = await NewClient().PostAsync("/api/v1/auth/2fa/setup", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
