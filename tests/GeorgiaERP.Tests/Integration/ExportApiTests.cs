using System.Net;
using FluentAssertions;
using Xunit;

namespace GeorgiaERP.Tests.Integration;

[Collection("Integration")]
public class ExportApiTests : IntegrationTestBase
{
    public ExportApiTests(ErpApiFactory factory) : base(factory) { }

    [Fact]
    public async Task ExportProducts_Authenticated_ReturnsCsvFile()
    {
        var client = await AuthenticatedClient("export-products-user", "export-products@test.com");

        var response = await client.GetAsync("/api/v1/export/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Should().HaveCountGreaterThan(3); // At least BOM + header

        // Verify UTF-8 BOM
        bytes[0].Should().Be(0xEF);
        bytes[1].Should().Be(0xBB);
        bytes[2].Should().Be(0xBF);

        var content = System.Text.Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        content.Should().Contain("SKU");
        content.Should().Contain("Name");
    }

    [Fact]
    public async Task ExportProducts_Unauthenticated_Returns401()
    {
        var client = NewClient();

        var response = await client.GetAsync("/api/v1/export/products");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExportInventory_Authenticated_ReturnsCsvFile()
    {
        var client = await AuthenticatedClient("export-inventory-user", "export-inventory@test.com");

        var response = await client.GetAsync("/api/v1/export/inventory");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("SKU");
        content.Should().Contain("Qty On Hand");
    }

    [Fact]
    public async Task ExportSales_Authenticated_ReturnsCsvFile()
    {
        var client = await AuthenticatedClient("export-sales-user", "export-sales@test.com");

        var response = await client.GetAsync("/api/v1/export/sales");

        // Read response for diagnostics before assertion
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"Response body: {content[..Math.Min(content.Length, 500)]}");
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");

        content.Should().Contain("Transaction #");
        content.Should().Contain("Total");
    }

    [Fact]
    public async Task ExportCustomers_Authenticated_ReturnsCsvFile()
    {
        var client = await AuthenticatedClient("export-customers-user", "export-customers@test.com");

        var response = await client.GetAsync("/api/v1/export/customers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Customer #");
        content.Should().Contain("Email");
    }

    [Fact]
    public async Task ExportAudit_Authenticated_ReturnsCsvFile()
    {
        var client = await AuthenticatedClient("export-audit-user", "export-audit@test.com");

        var response = await client.GetAsync("/api/v1/export/audit");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Entity Type");
        content.Should().Contain("Action");
    }

    [Fact]
    public async Task ExportProducts_WithFilters_ReturnsCsv()
    {
        var client = await AuthenticatedClient("export-filter-user", "export-filter@test.com");

        var response = await client.GetAsync("/api/v1/export/products?isActive=true&search=test");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
    }

    [Fact]
    public async Task ExportSales_WithDateRange_ReturnsCsv()
    {
        var client = await AuthenticatedClient("export-daterange-user", "export-daterange@test.com");

        var from = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(-30).ToString("o"));
        var to = Uri.EscapeDataString(DateTimeOffset.UtcNow.ToString("o"));

        var response = await client.GetAsync($"/api/v1/export/sales?from={from}&to={to}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
    }

    [Fact]
    public async Task ExportInventory_LowStockOnly_ReturnsCsv()
    {
        var client = await AuthenticatedClient("export-lowstock-user", "export-lowstock@test.com");

        var response = await client.GetAsync("/api/v1/export/inventory?lowStockOnly=true");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
    }
}
