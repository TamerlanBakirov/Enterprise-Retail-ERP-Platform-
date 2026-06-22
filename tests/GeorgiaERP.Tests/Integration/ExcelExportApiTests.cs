using System.Net;
using FluentAssertions;
using Xunit;

namespace GeorgiaERP.Tests.Integration;

[Collection("Integration")]
public class ExcelExportApiTests : IntegrationTestBase
{
    public ExcelExportApiTests(ErpApiFactory factory) : base(factory) { }

    [Fact]
    public async Task ExportProducts_FormatXlsx_ReturnsExcelFile()
    {
        var client = await AuthenticatedClient("excel-products-user", "excel-products@test.com");

        var response = await client.GetAsync("/api/v1/export/products?format=xlsx");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should()
            .Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Should().HaveCountGreaterThan(100); // Excel files are larger than CSV

        // Excel files start with PK (zip format)
        bytes[0].Should().Be(0x50); // 'P'
        bytes[1].Should().Be(0x4B); // 'K'
    }

    [Fact]
    public async Task ExportInventory_FormatExcel_ReturnsExcelFile()
    {
        var client = await AuthenticatedClient("excel-inventory-user", "excel-inventory@test.com");

        var response = await client.GetAsync("/api/v1/export/inventory?format=excel");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should()
            .Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    [Fact]
    public async Task ExportSales_FormatXlsx_ReturnsExcelFile()
    {
        var client = await AuthenticatedClient("excel-sales-user", "excel-sales@test.com");

        var response = await client.GetAsync("/api/v1/export/sales?format=xlsx");

        // Read content for diagnostics
        var content = await response.Content.ReadAsByteArrayAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"Response length: {content.Length}");
        response.Content.Headers.ContentType!.MediaType.Should()
            .Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    [Fact]
    public async Task ExportCustomers_FormatXlsx_ReturnsExcelFile()
    {
        var client = await AuthenticatedClient("excel-customers-user", "excel-customers@test.com");

        var response = await client.GetAsync("/api/v1/export/customers?format=xlsx");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should()
            .Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    [Fact]
    public async Task ExportAudit_FormatXlsx_ReturnsExcelFile()
    {
        var client = await AuthenticatedClient("excel-audit-user", "excel-audit@test.com");

        var response = await client.GetAsync("/api/v1/export/audit?format=xlsx");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should()
            .Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    [Fact]
    public async Task ExportProducts_DefaultFormat_ReturnsCsv()
    {
        var client = await AuthenticatedClient("excel-default-user", "excel-default@test.com");

        var response = await client.GetAsync("/api/v1/export/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
    }

    [Fact]
    public async Task ExportProducts_FormatCsv_ReturnsCsv()
    {
        var client = await AuthenticatedClient("excel-csv-user", "excel-csv@test.com");

        var response = await client.GetAsync("/api/v1/export/products?format=csv");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
    }
}
