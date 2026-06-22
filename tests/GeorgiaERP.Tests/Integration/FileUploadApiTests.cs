using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using GeorgiaERP.Domain.Identity;
using GeorgiaERP.Domain.Products;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GeorgiaERP.Tests.Integration;

[Collection("Integration")]
public class FileUploadApiTests : IntegrationTestBase
{
    public FileUploadApiTests(ErpApiFactory factory) : base(factory) { }

    private Task<HttpClient> AuthenticatedClient()
        => AuthenticatedClient("fileupload_admin", "fileupload@test.local", "File", "Admin", "ფაილი");

    private async Task<Guid> CreateTestProduct()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var category = Category.Create($"FC-{suffix}", $"File Test Category {suffix}");
        db.Categories.Add(category);

        var product = Product.Create($"FSK-{suffix}", $"File Test Product {suffix}", category.Id, "PCS");
        db.Products.Add(product);
        await db.SaveChangesAsync();

        return product.Id;
    }

    [Fact]
    public async Task UploadProductImage_WithValidImage_Returns201()
    {
        var client = await AuthenticatedClient();
        var productId = await CreateTestProduct();

        var imageContent = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 }; // JPEG magic bytes
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(imageContent);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "file", "test-image.jpg");

        var response = await client.PostAsync($"/api/v1/products/{productId}/image", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("fileName").GetString().Should().Be("test-image.jpg");
        body.GetProperty("contentType").GetString().Should().Be("image/jpeg");
        body.GetProperty("category").GetString().Should().Be("product-image");
    }

    [Fact]
    public async Task UploadProductImage_WithNonExistentProduct_Returns404()
    {
        var client = await AuthenticatedClient();
        var fakeProductId = Guid.NewGuid();

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "test.png");

        var response = await client.PostAsync($"/api/v1/products/{fakeProductId}/image", content);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UploadProductImage_WithInvalidFileType_Returns400()
    {
        var client = await AuthenticatedClient();
        var productId = await CreateTestProduct();

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("not an image"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", "test.txt");

        var response = await client.PostAsync($"/api/v1/products/{productId}/image", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadProductImage_WithNoFile_Returns400()
    {
        var client = await AuthenticatedClient();
        var productId = await CreateTestProduct();

        var content = new MultipartFormDataContent();

        var response = await client.PostAsync($"/api/v1/products/{productId}/image", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadDocument_WithValidFile_Returns201()
    {
        var client = await AuthenticatedClient();

        var pdfContent = Encoding.UTF8.GetBytes("%PDF-1.4 test content");
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(pdfContent);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "test-doc.pdf");

        var response = await client.PostAsync("/api/v1/documents/upload?category=invoice", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("fileName").GetString().Should().Be("test-doc.pdf");
        body.GetProperty("contentType").GetString().Should().Be("application/pdf");
        body.GetProperty("category").GetString().Should().Be("invoice");
    }

    [Fact]
    public async Task UploadDocument_WithEntityLink_Returns201()
    {
        var client = await AuthenticatedClient();
        var entityId = Guid.NewGuid();

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("CSV,data,here"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "data.csv");

        var response = await client.PostAsync(
            $"/api/v1/documents/upload?category=report&entityId={entityId}&entityType=PurchaseOrder", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("entityId").GetString().Should().Be(entityId.ToString());
        body.GetProperty("entityType").GetString().Should().Be("PurchaseOrder");
    }

    [Fact]
    public async Task GetFile_AfterUpload_ReturnsFileContent()
    {
        var client = await AuthenticatedClient();

        // Upload a file first
        var fileBytes = Encoding.UTF8.GetBytes("test content for download");
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", "download-test.txt");

        var uploadResponse = await client.PostAsync("/api/v1/documents/upload", content);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await uploadResponse.Content.ReadFromJsonAsync<JsonElement>();
        var fileId = body.GetProperty("id").GetString();

        // Download the file
        var downloadResponse = await client.GetAsync($"/api/v1/files/{fileId}");

        downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        downloadResponse.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");
    }

    [Fact]
    public async Task GetFile_WithNonExistentId_Returns404()
    {
        var client = await AuthenticatedClient();

        var response = await client.GetAsync($"/api/v1/files/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UploadProductImage_SetsImageUrlOnProduct()
    {
        var client = await AuthenticatedClient();
        var productId = await CreateTestProduct();

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 0x89, 0x50, 0x4E, 0x47 }); // PNG magic bytes
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "product-photo.png");

        var uploadResponse = await client.PostAsync($"/api/v1/products/{productId}/image", content);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify the product now has an ImageUrl
        var productResponse = await client.GetAsync($"/api/v1/products/{productId}");
        productResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var product = await productResponse.Content.ReadFromJsonAsync<JsonElement>();
        product.GetProperty("imageUrl").GetString().Should().StartWith("/api/v1/files/");
    }

    [Fact]
    public async Task UploadDocument_WithoutAuth_Returns401()
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "test.pdf");

        var response = await NewClient().PostAsync("/api/v1/documents/upload", content);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
