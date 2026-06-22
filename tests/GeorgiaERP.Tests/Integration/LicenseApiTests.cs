using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GeorgiaERP.Infrastructure.Licensing;
using Xunit;

namespace GeorgiaERP.Tests.Integration;

[Collection("Integration")]
public class LicenseApiTests : IntegrationTestBase
{
    private const string TestSigningKey = "TEST-LICENSE-SIGNING-KEY-WITH-AT-LEAST-THIRTY-TWO-CHARS";

    public LicenseApiTests(ErpApiFactory factory) : base(factory) { }

    private Task<HttpClient> AuthenticatedClient()
        => AuthenticatedClient("lic_admin", "licadmin@test.local", "Lic", "Admin", "ლიცენზია");

    private static string GenerateValidKey(string company = "Test Company")
        => HmacLicenseKeyValidator.CreateKey(TestSigningKey, company, DateTimeOffset.UtcNow.AddYears(1), 50, 10);

    // === Status ===

    [Fact]
    public async Task GetStatus_Anonymous_ReturnsOk()
    {
        var client = NewClient();

        var response = await client.GetAsync("/api/v1/license/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // === Activate ===

    [Fact]
    public async Task Activate_ValidKey_ReturnsOk()
    {
        var client = NewClient();
        var key = GenerateValidKey();

        var response = await client.PostAsJsonAsync("/api/v1/license/activate", new
        {
            licenseKey = key,
            companyName = "Test Company",
            contactEmail = "test@test.local"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Activate_InvalidKey_ReturnsBadRequest()
    {
        var client = NewClient();

        var response = await client.PostAsJsonAsync("/api/v1/license/activate", new
        {
            licenseKey = "totally-bogus-key",
            companyName = "Test Company",
            contactEmail = "test@test.local"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Activate_SameKeyTwice_ReturnsExisting()
    {
        var client = NewClient();
        var key = GenerateValidKey("Dup Company");

        await client.PostAsJsonAsync("/api/v1/license/activate", new
        {
            licenseKey = key,
            companyName = "Dup Company",
            contactEmail = "dup@test.local"
        });

        var response = await client.PostAsJsonAsync("/api/v1/license/activate", new
        {
            licenseKey = key,
            companyName = "Dup Company",
            contactEmail = "dup@test.local"
        });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    // === Deactivate ===

    [Fact]
    public async Task Deactivate_Unauthenticated_Returns401()
    {
        var client = NewClient();

        var response = await client.PostAsync("/api/v1/license/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Deactivate_Authenticated_ReturnsResult()
    {
        var client = await AuthenticatedClient();

        var response = await client.PostAsync("/api/v1/license/deactivate", null);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    // === Renew ===

    [Fact]
    public async Task Renew_Unauthenticated_Returns401()
    {
        var client = NewClient();

        var response = await client.PostAsJsonAsync("/api/v1/license/renew", new
        {
            licenseKey = "TEST-KEY-001"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
