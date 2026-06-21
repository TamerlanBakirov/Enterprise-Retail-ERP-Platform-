using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Identity;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace GeorgiaERP.Tests.Integration;

/// <summary>
/// Base class for integration tests that need authenticated HTTP clients.
/// Extracts the duplicated AuthenticatedClient() / NewClient() pattern
/// shared across all integration test classes.
/// </summary>
public abstract class IntegrationTestBase
{
    protected readonly ErpApiFactory Factory;

    protected IntegrationTestBase(ErpApiFactory factory) => Factory = factory;

    protected HttpClient NewClient() => Factory.CreateClient();

    /// <summary>
    /// Creates an authenticated HTTP client with a super_admin user.
    /// Each test class should use a unique username to avoid cross-test interference.
    /// </summary>
    protected async Task<HttpClient> AuthenticatedClient(
        string username,
        string email,
        string firstName = "Test",
        string lastName = "Admin",
        string? firstNameKa = null,
        string? lastNameKa = "ადმინი")
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();

        if (!db.Users.Any(u => u.Username == username))
        {
            var role = db.Roles.FirstOrDefault(r => r.Code == "super_admin");
            if (role is null)
            {
                role = Role.Create("super_admin", "Super Admin", "სუპერ ადმინი", "Full access", true);
                db.Roles.Add(role);
            }

            var user = User.Create(username, email,
                passwordService.HashPassword("Admin@123!"),
                firstName, lastName, firstNameKa, lastNameKa, "ka");
            db.Users.Add(user);
            db.UserRoles.Add(UserRole.Create(user.Id, role.Id));
            await db.SaveChangesAsync();
        }

        var client = NewClient();
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { username, password = "Admin@123!" });

        if (loginResponse.StatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException(
                $"Failed to authenticate as '{username}': {loginResponse.StatusCode}");

        var body = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
