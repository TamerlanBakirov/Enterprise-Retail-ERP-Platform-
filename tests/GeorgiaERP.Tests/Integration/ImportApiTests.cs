using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GeorgiaERP.Tests.Integration;

[Collection("Integration")]
public class ImportApiTests : IntegrationTestBase
{
    public ImportApiTests(ErpApiFactory factory) : base(factory) { }

    [Fact]
    public async Task ImportProducts_Unauthenticated_Returns401()
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("SKU,Name\nIMPORT-001,Test"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "products.csv");

        var response = await NewClient().PostAsync("/api/v1/import/products", content);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ImportProducts_EmptyFile_ReturnsBadRequest()
    {
        var client = await AuthenticatedClient("import-empty-user", "import-empty@test.com");

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Array.Empty<byte>());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "products.csv");

        var response = await client.PostAsync("/api/v1/import/products", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ImportProducts_ValidCsv_ReturnsImportResult()
    {
        var client = await AuthenticatedClient("import-valid-user", "import-valid@test.com");

        // Get a valid category ID
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var category = db.Categories.FirstOrDefault();
        category.Should().NotBeNull("Seeded categories should exist");

        var csv = $"SKU,Name,CategoryId,UnitOfMeasure,VatApplicable\nIMPORT-INT-001,Integration Test Product,{category!.Id},pcs,true";

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "products.csv");

        var response = await client.PostAsync("/api/v1/import/products", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalRows").GetInt32().Should().Be(1);
        body.GetProperty("successCount").GetInt32().Should().Be(1);
        body.GetProperty("errorCount").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task ImportProducts_InvalidData_ReturnsErrors()
    {
        var client = await AuthenticatedClient("import-invalid-user", "import-invalid@test.com");

        // Missing required fields
        var csv = "SKU,Name,CategoryId,UnitOfMeasure\n,,invalid-guid,";

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "products.csv");

        var response = await client.PostAsync("/api/v1/import/products", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errorCount").GetInt32().Should().BeGreaterThan(0);
        body.GetProperty("errors").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ImportInventory_Unauthenticated_Returns401()
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("SKU,WarehouseId,Quantity\nP001,id,10"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "inventory.csv");

        var response = await NewClient().PostAsync("/api/v1/import/inventory", content);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ImportInventory_InvalidSku_ReturnsErrors()
    {
        var client = await AuthenticatedClient("import-inv-invalid-user", "import-inv-invalid@test.com");

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var warehouse = db.Warehouses.FirstOrDefault();

        var warehouseId = warehouse?.Id ?? Guid.NewGuid();
        var csv = $"SKU,WarehouseId,Quantity\nNONEXISTENT-SKU,{warehouseId},10";

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "inventory.csv");

        var response = await client.PostAsync("/api/v1/import/inventory", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errorCount").GetInt32().Should().BeGreaterThan(0);
    }
}
