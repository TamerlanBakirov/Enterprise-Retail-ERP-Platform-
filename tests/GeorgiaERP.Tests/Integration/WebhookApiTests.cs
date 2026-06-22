using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace GeorgiaERP.Tests.Integration;

[Collection("Integration")]
public class WebhookApiTests : IntegrationTestBase
{
    public WebhookApiTests(ErpApiFactory factory) : base(factory) { }

    private Task<HttpClient> AuthenticatedClient()
        => AuthenticatedClient("webhook_admin", "webhook@test.local", "Webhook", "Admin", "ვებჰუკ");

    [Fact]
    public async Task CreateWebhook_WithValidData_Returns201()
    {
        var client = await AuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/v1/webhooks", new
        {
            name = "Test Webhook",
            url = "https://example.com/webhook",
            secret = "test-secret-key",
            eventTypes = new[] { "order.created", "stock.low" },
            maxRetries = 3
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("name").GetString().Should().Be("Test Webhook");
        body.GetProperty("url").GetString().Should().Be("https://example.com/webhook");
        body.GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetWebhooks_ReturnsWebhookList()
    {
        var client = await AuthenticatedClient();

        // Create a webhook first
        await client.PostAsJsonAsync("/api/v1/webhooks", new
        {
            name = "List Test Webhook",
            url = "https://example.com/list-test",
            secret = "secret123",
            eventTypes = new[] { "product.created" }
        });

        var response = await client.GetAsync("/api/v1/webhooks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetWebhookById_AfterCreate_ReturnsWebhook()
    {
        var client = await AuthenticatedClient();

        var createResponse = await client.PostAsJsonAsync("/api/v1/webhooks", new
        {
            name = "Get By Id Test",
            url = "https://example.com/get-test",
            secret = "secret",
            eventTypes = new[] { "*" }
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/v1/webhooks/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("name").GetString().Should().Be("Get By Id Test");
    }

    [Fact]
    public async Task UpdateWebhook_ChangesFields()
    {
        var client = await AuthenticatedClient();

        var createResponse = await client.PostAsJsonAsync("/api/v1/webhooks", new
        {
            name = "Update Test",
            url = "https://example.com/update",
            secret = "secret",
            eventTypes = new[] { "event1" }
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/webhooks/{id}", new
        {
            name = "Updated Name",
            eventTypes = new[] { "event1", "event2" }
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify
        var getResponse = await client.GetAsync($"/api/v1/webhooks/{id}");
        var body = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("name").GetString().Should().Be("Updated Name");
    }

    [Fact]
    public async Task DeleteWebhook_ReturnsNoContent()
    {
        var client = await AuthenticatedClient();

        var createResponse = await client.PostAsJsonAsync("/api/v1/webhooks", new
        {
            name = "Delete Test",
            url = "https://example.com/delete",
            secret = "secret",
            eventTypes = new[] { "event" }
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();

        var deleteResponse = await client.DeleteAsync($"/api/v1/webhooks/{id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's gone
        var getResponse = await client.GetAsync($"/api/v1/webhooks/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeactivateWebhook_SetsInactive()
    {
        var client = await AuthenticatedClient();

        var createResponse = await client.PostAsJsonAsync("/api/v1/webhooks", new
        {
            name = "Deactivate Test",
            url = "https://example.com/deactivate",
            secret = "secret",
            eventTypes = new[] { "event" }
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();

        var response = await client.PostAsync($"/api/v1/webhooks/{id}/deactivate", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await client.GetAsync($"/api/v1/webhooks/{id}");
        var body = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("isActive").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task ActivateWebhook_SetsActive()
    {
        var client = await AuthenticatedClient();

        var createResponse = await client.PostAsJsonAsync("/api/v1/webhooks", new
        {
            name = "Activate Test",
            url = "https://example.com/activate",
            secret = "secret",
            eventTypes = new[] { "event" }
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();

        await client.PostAsync($"/api/v1/webhooks/{id}/deactivate", null);
        var response = await client.PostAsync($"/api/v1/webhooks/{id}/activate", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await client.GetAsync($"/api/v1/webhooks/{id}");
        var body = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetDeliveryLogs_ReturnsEmptyForNewWebhook()
    {
        var client = await AuthenticatedClient();

        var createResponse = await client.PostAsJsonAsync("/api/v1/webhooks", new
        {
            name = "Logs Test",
            url = "https://example.com/logs",
            secret = "secret",
            eventTypes = new[] { "event" }
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/v1/webhooks/{id}/deliveries");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalCount").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task WebhookEndpoints_WithoutAuth_Returns401()
    {
        var response = await NewClient().GetAsync("/api/v1/webhooks");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetNonExistentWebhook_Returns404()
    {
        var client = await AuthenticatedClient();
        var response = await client.GetAsync($"/api/v1/webhooks/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
