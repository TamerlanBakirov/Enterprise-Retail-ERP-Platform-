using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GeorgiaERP.Domain.Products;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GeorgiaERP.Tests.Integration;

[Collection("Integration")]
public class ProductsApiTests : IntegrationTestBase
{
    public ProductsApiTests(ErpApiFactory factory) : base(factory) { }

    private Task<HttpClient> AuthenticatedClient()
        => AuthenticatedClient("prod_admin", "prodadmin@test.local", "Prod", "Admin", "პროდ");

    private async Task<Guid> SeedCategory(string? code = null)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cat = Category.Create(code ?? $"CAT-{Guid.NewGuid():N}"[..10], "Test Category");
        db.Categories.Add(cat);
        await db.SaveChangesAsync();
        return cat.Id;
    }

    private async Task<Guid> SeedProduct(Guid? categoryId = null)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var catId = categoryId ?? await SeedCategory();
        if (categoryId is null)
        {
            var cat = db.Categories.FirstOrDefault();
            if (cat is not null) catId = cat.Id;
        }

        var product = Product.Create(
            $"SKU-{Guid.NewGuid():N}"[..15], "Test Product", catId, "Piece");
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product.Id;
    }

    // === Auth ===

    [Fact]
    public async Task Products_WithoutAuth_Returns401()
    {
        var response = await NewClient().GetAsync("/api/v1/products");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // === GET ===

    [Fact]
    public async Task GetProducts_ReturnsOk()
    {
        var client = await AuthenticatedClient();
        var catId = await SeedCategory();
        await SeedProduct(catId);

        var response = await client.GetAsync("/api/v1/products?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetProducts_WithSearch_FiltersResults()
    {
        var client = await AuthenticatedClient();

        var response = await client.GetAsync("/api/v1/products?search=NONEXISTENT_PRODUCT_XYZ&page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetProducts_ByCategoryId_Works()
    {
        var client = await AuthenticatedClient();
        var catId = await SeedCategory();
        await SeedProduct(catId);

        var response = await client.GetAsync($"/api/v1/products?categoryId={catId}&page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // === GET by ID ===

    [Fact]
    public async Task GetProduct_ById_ReturnsOk()
    {
        var client = await AuthenticatedClient();
        var catId = await SeedCategory();
        var productId = await SeedProduct(catId);

        var response = await client.GetAsync($"/api/v1/products/{productId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetString().Should().Be(productId.ToString());
    }

    [Fact]
    public async Task GetProduct_NotFound_Returns404()
    {
        var client = await AuthenticatedClient();

        var response = await client.GetAsync($"/api/v1/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // === POST ===

    [Fact]
    public async Task CreateProduct_ReturnsCreated()
    {
        var client = await AuthenticatedClient();
        var catId = await SeedCategory();

        var response = await client.PostAsJsonAsync("/api/v1/products", new
        {
            sku = $"NEW-{Guid.NewGuid():N}"[..15],
            name = "New Product",
            nameKa = "ახალი პროდუქტი",
            description = "Test description",
            categoryId = catId,
            unitOfMeasure = "Piece",
            vatApplicable = true,
            weightKg = 1.5m,
            isSerialized = false,
            isBatchTracked = false,
            hasExpiry = false,
            barcodes = new[]
            {
                new { barcode = $"BC-{Guid.NewGuid():N}"[..13], barcodeType = "EAN13", isPrimary = true }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("name").GetString().Should().Be("New Product");
    }

    [Fact]
    public async Task CreateProduct_DuplicateSku_ReturnsBadRequest()
    {
        var client = await AuthenticatedClient();
        var catId = await SeedCategory();
        var sku = $"DUP-{Guid.NewGuid():N}"[..15];

        await client.PostAsJsonAsync("/api/v1/products", new
        {
            sku,
            name = "First Product",
            categoryId = catId,
            unitOfMeasure = "Piece",
            vatApplicable = true,
            isSerialized = false,
            isBatchTracked = false,
            hasExpiry = false
        });

        var response = await client.PostAsJsonAsync("/api/v1/products", new
        {
            sku,
            name = "Duplicate Product",
            categoryId = catId,
            unitOfMeasure = "Piece",
            vatApplicable = true,
            isSerialized = false,
            isBatchTracked = false,
            hasExpiry = false
        });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
    }

    // === PUT ===

    [Fact]
    public async Task UpdateProduct_ReturnsOk()
    {
        var client = await AuthenticatedClient();
        var catId = await SeedCategory();

        var createResponse = await client.PostAsJsonAsync("/api/v1/products", new
        {
            sku = $"UPD-{Guid.NewGuid():N}"[..15],
            name = "Original Name",
            categoryId = catId,
            unitOfMeasure = "Piece",
            vatApplicable = true,
            isSerialized = false,
            isBatchTracked = false,
            hasExpiry = false
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var productId = created.GetProperty("id").GetString()!;

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/products/{productId}", new
        {
            name = "Updated Name",
            nameKa = "განახლებული სახელი"
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // === DELETE ===

    [Fact]
    public async Task DeleteProduct_ReturnsNoContent()
    {
        var client = await AuthenticatedClient();
        var catId = await SeedCategory();

        var createResponse = await client.PostAsJsonAsync("/api/v1/products", new
        {
            sku = $"DEL-{Guid.NewGuid():N}"[..15],
            name = "Product to Delete",
            categoryId = catId,
            unitOfMeasure = "Piece",
            vatApplicable = true,
            isSerialized = false,
            isBatchTracked = false,
            hasExpiry = false
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var productId = created.GetProperty("id").GetString()!;

        var deleteResponse = await client.DeleteAsync($"/api/v1/products/{productId}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteProduct_NotFound_Returns404()
    {
        var client = await AuthenticatedClient();

        var response = await client.DeleteAsync($"/api/v1/products/{Guid.NewGuid()}");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    // === Categories ===

    [Fact]
    public async Task GetCategories_ReturnsOk()
    {
        var client = await AuthenticatedClient();
        await SeedCategory();

        var response = await client.GetAsync("/api/v1/products/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
