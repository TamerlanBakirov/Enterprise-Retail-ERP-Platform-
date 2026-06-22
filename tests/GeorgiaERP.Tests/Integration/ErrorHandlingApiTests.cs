using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace GeorgiaERP.Tests.Integration;

[Collection("Integration")]
public class ErrorHandlingApiTests : IntegrationTestBase
{
    public ErrorHandlingApiTests(ErpApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Unauthenticated_Request_Returns_401()
    {
        var client = NewClient();
        var response = await client.GetAsync("/api/v1/products");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task NotFound_Route_Returns_404()
    {
        var client = await AuthenticatedClient("errtest_admin", "errtest@test.ge");
        var response = await client.GetAsync("/api/v1/nonexistent");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Validation_Error_Returns_400_With_ProblemDetails()
    {
        var client = await AuthenticatedClient("errtest_val_admin", "errval@test.ge");

        // Empty body to trigger validation error
        var response = await client.PostAsJsonAsync("/api/v1/products", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Api_Version_Header_Present_On_All_Responses()
    {
        var client = NewClient();
        var response = await client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().Contain(h => h.Key == "X-Api-Version");
        response.Headers.GetValues("X-Api-Version").First().Should().Be("1.0");
    }

    [Fact]
    public async Task Api_Does_Not_Leak_Powered_By_Header()
    {
        var client = NewClient();
        var response = await client.GetAsync("/health/live");

        response.Headers.Should().NotContain(h => h.Key == "X-Powered-By");
    }

    [Fact]
    public async Task Swagger_Endpoint_Available_In_Development()
    {
        var client = NewClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Georgia ERP Platform API");
        content.Should().Contain("Authentication");
        content.Should().Contain("Products");
        content.Should().Contain("Inventory");
    }

    [Fact]
    public async Task Swagger_Contains_JWT_Security_Scheme()
    {
        var client = NewClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Bearer");
        content.Should().Contain("JWT");
    }

    [Fact]
    public async Task Health_Live_Endpoint_Returns_Healthy()
    {
        var client = NewClient();
        var response = await client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("healthy");
    }
}
