using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace GeorgiaERP.Tests.Integration;

[Collection("Integration")]
public class UpdatesApiTests : IntegrationTestBase
{
    public UpdatesApiTests(ErpApiFactory factory) : base(factory) { }

    private Task<HttpClient> AuthenticatedClient()
        => AuthenticatedClient("upd_admin", "updadmin@test.local", "Upd", "Admin", "განახლება");

    [Fact]
    public async Task GetLatestVersion_Unauthenticated_Returns401()
    {
        var client = NewClient();

        var response = await client.GetAsync("/api/v1/updates/latest");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetLatestVersion_Authenticated_ReturnsVersionInfo()
    {
        var client = await AuthenticatedClient();

        var response = await client.GetAsync("/api/v1/updates/latest");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("version").GetString().Should().NotBeNullOrEmpty();
    }
}
