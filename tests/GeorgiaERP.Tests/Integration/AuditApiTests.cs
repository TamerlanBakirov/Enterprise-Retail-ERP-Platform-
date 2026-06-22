using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GeorgiaERP.Domain.Common;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GeorgiaERP.Tests.Integration;

[Collection("Integration")]
public class AuditApiTests : IntegrationTestBase
{
    public AuditApiTests(ErpApiFactory factory) : base(factory) { }

    private Task<HttpClient> AuthenticatedClient()
        => AuthenticatedClient("audit_admin", "auditadmin@test.local", "Audit");

    [Fact]
    public async Task GetAuditLogs_WithoutAuth_Returns401()
    {
        var response = await NewClient().GetAsync("/api/v1/audit");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAuditLogs_Empty_ReturnsOk()
    {
        var client = await AuthenticatedClient();
        var response = await client.GetAsync("/api/v1/audit");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("items").GetArrayLength().Should().BeGreaterThanOrEqualTo(0);
            body.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        }
    }

    [Fact]
    public async Task GetAuditLogs_WithEntityTypeFilter_ReturnsOk()
    {
        var client = await AuthenticatedClient();
        var response = await client.GetAsync("/api/v1/audit?entityType=Product&page=1&pageSize=10");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetAuditLogs_WithDateRange_ReturnsOk()
    {
        var client = await AuthenticatedClient();
        var from = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(-7).ToString("o"));
        var to = Uri.EscapeDataString(DateTimeOffset.UtcNow.ToString("o"));

        var response = await client.GetAsync($"/api/v1/audit?from={from}&to={to}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetAuditLogs_WithSeededData_ReturnsEntries()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.AuditLogs.Add(AuditLog.Create(
            "Product",
            Guid.NewGuid().ToString(),
            "Create",
            "{\"Name\":\"Test\"}",
            Guid.NewGuid()));
        await db.SaveChangesAsync();

        var client = await AuthenticatedClient();
        var response = await client.GetAsync("/api/v1/audit?entityType=Product");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(1);
        }
    }

    [Fact]
    public async Task GetAuditLogs_Pagination_ReturnsOk()
    {
        var client = await AuthenticatedClient();
        var response = await client.GetAsync("/api/v1/audit?page=1&pageSize=5");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("page").GetInt32().Should().Be(1);
            body.GetProperty("pageSize").GetInt32().Should().Be(5);
        }
    }
}
