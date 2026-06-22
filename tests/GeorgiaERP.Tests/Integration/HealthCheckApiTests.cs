using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace GeorgiaERP.Tests.Integration;

[Collection("Integration")]
public class HealthCheckApiTests : IntegrationTestBase
{
    public HealthCheckApiTests(ErpApiFactory factory) : base(factory) { }

    [Fact]
    public async Task HealthLive_ReturnsHealthy()
    {
        var response = await NewClient().GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("healthy");
    }

    [Fact]
    public async Task HealthLive_DoesNotIncludeDependencyChecks()
    {
        var response = await NewClient().GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        // Liveness should be minimal - just status
        body.Should().Contain("status");
        body.Should().NotContain("entries");
    }

    [Fact]
    public async Task HealthReady_ReturnsOk()
    {
        var response = await NewClient().GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_LegacyEndpoint_ReturnsOk()
    {
        var response = await NewClient().GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthReady_IncludesDetailedReport()
    {
        var response = await NewClient().GetAsync("/health/ready");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        body.GetProperty("status").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("totalDuration").GetString().Should().NotBeNullOrEmpty();
    }
}
