using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Identity;
using GeorgiaERP.Domain.Organization;
using GeorgiaERP.Domain.Products;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using WarehouseEntity = GeorgiaERP.Domain.Organization.Warehouse;

namespace GeorgiaERP.Tests.Integration;

[Collection("Integration")]
public class WarehouseApiTests
{
    private readonly ErpApiFactory _factory;

    public WarehouseApiTests(ErpApiFactory factory) => _factory = factory;

    private HttpClient NewClient() => _factory.CreateClient();

    private async Task<HttpClient> AuthenticatedClient()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();

        if (!db.Users.Any(u => u.Username == "wh_admin"))
        {
            var role = db.Roles.FirstOrDefault(r => r.Code == "super_admin");
            if (role is null)
            {
                role = Role.Create("super_admin", "Super Admin", "სუპერ ადმინი", "Full access", true);
                db.Roles.Add(role);
            }

            var user = User.Create("wh_admin", "whadmin@test.local",
                passwordService.HashPassword("Admin@123!"),
                "WH", "Admin", "საწყობი", "ადმინი", "ka");
            db.Users.Add(user);
            db.UserRoles.Add(UserRole.Create(user.Id, role.Id));
            await db.SaveChangesAsync();
        }

        var client = NewClient();
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { username = "wh_admin", password = "Admin@123!" });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<Guid> SeedWarehouse()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var wh = WarehouseEntity.Create($"WH-{Guid.NewGuid():N}"[..15], "Test Warehouse", WarehouseType.Central);
        db.Warehouses.Add(wh);
        await db.SaveChangesAsync();
        return wh.Id;
    }

    private async Task<Guid> SeedProduct()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var category = db.Categories.FirstOrDefault();
        if (category is null)
        {
            category = Category.Create($"CAT-{Guid.NewGuid():N}"[..10], "Test Category");
            db.Categories.Add(category);
            await db.SaveChangesAsync();
        }

        var product = Product.Create(
            $"SKU-{Guid.NewGuid():N}"[..15], "Test Product", category.Id, "Piece");
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product.Id;
    }

    // === Warehouse CRUD ===

    [Fact]
    public async Task Warehouse_WithoutAuth_Returns401()
    {
        var response = await NewClient().GetAsync($"/api/v1/warehouse/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateWarehouse_ReturnsCreated()
    {
        var client = await AuthenticatedClient();
        var code = $"WH-{Guid.NewGuid():N}"[..15];

        var response = await client.PostAsJsonAsync("/api/v1/warehouse",
            new { code, name = "New Warehouse", warehouseType = "Central" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateWarehouse_DuplicateCode_ReturnsConflict()
    {
        var client = await AuthenticatedClient();
        var code = $"WH-{Guid.NewGuid():N}"[..15];

        await client.PostAsJsonAsync("/api/v1/warehouse",
            new { code, name = "First", warehouseType = "Central" });

        var response = await client.PostAsJsonAsync("/api/v1/warehouse",
            new { code, name = "Second", warehouseType = "Regional" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetWarehouse_ReturnsDetail()
    {
        var client = await AuthenticatedClient();
        var warehouseId = await SeedWarehouse();

        var response = await client.GetAsync($"/api/v1/warehouse/{warehouseId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("code").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetWarehouse_NotFound_Returns404()
    {
        var client = await AuthenticatedClient();

        var response = await client.GetAsync($"/api/v1/warehouse/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateWarehouse_ReturnsOk()
    {
        var client = await AuthenticatedClient();
        var warehouseId = await SeedWarehouse();

        var response = await client.PutAsJsonAsync($"/api/v1/warehouse/{warehouseId}",
            new { name = "Updated Warehouse", address = "New Address", city = "Tbilisi", region = "Tbilisi" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeactivateWarehouse_ReturnsOk()
    {
        var client = await AuthenticatedClient();
        var warehouseId = await SeedWarehouse();

        var response = await client.PostAsync($"/api/v1/warehouse/{warehouseId}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ActivateWarehouse_ReturnsOk()
    {
        var client = await AuthenticatedClient();
        var warehouseId = await SeedWarehouse();

        await client.PostAsync($"/api/v1/warehouse/{warehouseId}/deactivate", null);
        var response = await client.PostAsync($"/api/v1/warehouse/{warehouseId}/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // === Locations ===

    [Fact]
    public async Task CreateLocation_ReturnsCreated()
    {
        var client = await AuthenticatedClient();
        var warehouseId = await SeedWarehouse();
        var code = $"LOC-{Guid.NewGuid():N}"[..10];

        var response = await client.PostAsJsonAsync($"/api/v1/warehouse/{warehouseId}/locations",
            new { code, name = "Zone A", locationType = "Zone", sortOrder = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GetLocations_ReturnsList()
    {
        var client = await AuthenticatedClient();
        var warehouseId = await SeedWarehouse();

        var response = await client.GetAsync($"/api/v1/warehouse/{warehouseId}/locations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // === Receiving Orders ===

    [Fact]
    public async Task CreateReceivingOrder_ReturnsCreated()
    {
        var client = await AuthenticatedClient();
        var warehouseId = await SeedWarehouse();
        var productId = await SeedProduct();

        var response = await client.PostAsJsonAsync("/api/v1/warehouse/receiving", new
        {
            warehouseId,
            source = "Manual",
            lines = new[] { new { productId, expectedQty = 100m } }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("receivingNumber").GetString().Should().StartWith("RCV-");
    }

    [Fact]
    public async Task GetReceivingOrderById_AfterCreate_ReturnsOrder()
    {
        var client = await AuthenticatedClient();
        var warehouseId = await SeedWarehouse();
        var productId = await SeedProduct();

        var createResponse = await client.PostAsJsonAsync("/api/v1/warehouse/receiving", new
        {
            warehouseId,
            source = "Manual",
            lines = new[] { new { productId, expectedQty = 10m } }
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = created.GetProperty("id").GetString()!;

        var response = await client.GetAsync($"/api/v1/warehouse/receiving/{orderId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetString().Should().Be("Expected");
        body.GetProperty("lines").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task ReceivingOrder_FullWorkflow()
    {
        var client = await AuthenticatedClient();
        var warehouseId = await SeedWarehouse();
        var productId = await SeedProduct();

        // Create
        var createResponse = await client.PostAsJsonAsync("/api/v1/warehouse/receiving", new
        {
            warehouseId,
            source = "PurchaseOrder",
            lines = new[] { new { productId, expectedQty = 50m } }
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = Guid.Parse(created.GetProperty("id").GetString()!);

        // Get by ID
        var getResponse = await client.GetAsync($"/api/v1/warehouse/receiving/{orderId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var order = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        order.GetProperty("status").GetString().Should().Be("Expected");
        var lineId = order.GetProperty("lines")[0].GetProperty("id").GetString()!;

        // Start receiving
        var startResponse = await client.PostAsync($"/api/v1/warehouse/receiving/{orderId}/start", null);
        startResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Receive line
        var receiveResponse = await client.PostAsJsonAsync(
            $"/api/v1/warehouse/receiving/{orderId}/lines/{lineId}/receive",
            new { receivedQty = 50m });
        receiveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Complete
        var completeResponse = await client.PostAsync($"/api/v1/warehouse/receiving/{orderId}/complete", null);
        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CancelReceivingOrder_Works()
    {
        var client = await AuthenticatedClient();
        var warehouseId = await SeedWarehouse();
        var productId = await SeedProduct();

        var createResponse = await client.PostAsJsonAsync("/api/v1/warehouse/receiving", new
        {
            warehouseId,
            source = "Manual",
            lines = new[] { new { productId, expectedQty = 10m } }
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = created.GetProperty("id").GetString()!;

        var response = await client.PostAsync($"/api/v1/warehouse/receiving/{orderId}/cancel", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // === Shipping Orders ===

    [Fact]
    public async Task CreateShippingOrder_ReturnsCreated()
    {
        var client = await AuthenticatedClient();
        var warehouseId = await SeedWarehouse();
        var productId = await SeedProduct();

        var response = await client.PostAsJsonAsync("/api/v1/warehouse/shipping", new
        {
            warehouseId,
            orderType = "SalesOrder",
            shippingAddress = "123 Main St",
            lines = new[] { new { productId, orderedQty = 25m } }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("shippingNumber").GetString().Should().StartWith("SHP-");
    }

    [Fact]
    public async Task GetShippingOrderById_AfterCreate_ReturnsOrder()
    {
        var client = await AuthenticatedClient();
        var warehouseId = await SeedWarehouse();
        var productId = await SeedProduct();

        var createResponse = await client.PostAsJsonAsync("/api/v1/warehouse/shipping", new
        {
            warehouseId,
            orderType = "Manual",
            lines = new[] { new { productId, orderedQty = 10m } }
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = created.GetProperty("id").GetString()!;

        var response = await client.GetAsync($"/api/v1/warehouse/shipping/{orderId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetString().Should().Be("Draft");
        body.GetProperty("lines").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task ShippingOrder_FullWorkflow()
    {
        var client = await AuthenticatedClient();
        var warehouseId = await SeedWarehouse();
        var productId = await SeedProduct();

        // Create
        var createResponse = await client.PostAsJsonAsync("/api/v1/warehouse/shipping", new
        {
            warehouseId,
            orderType = "SalesOrder",
            lines = new[] { new { productId, orderedQty = 30m } }
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = Guid.Parse(created.GetProperty("id").GetString()!);

        // Get by ID
        var getResponse = await client.GetAsync($"/api/v1/warehouse/shipping/{orderId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var order = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        order.GetProperty("status").GetString().Should().Be("Draft");
        var lineId = order.GetProperty("lines")[0].GetProperty("id").GetString()!;

        // Start picking
        var pickStartResponse = await client.PostAsync($"/api/v1/warehouse/shipping/{orderId}/pick", null);
        pickStartResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Pick line
        var pickLineResponse = await client.PostAsJsonAsync(
            $"/api/v1/warehouse/shipping/{orderId}/lines/{lineId}/pick",
            new { pickedQty = 30m });
        pickLineResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Pack
        var packResponse = await client.PostAsync($"/api/v1/warehouse/shipping/{orderId}/pack", null);
        packResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Ship
        var shipResponse = await client.PostAsJsonAsync($"/api/v1/warehouse/shipping/{orderId}/ship",
            new { trackingNumber = "TRACK-001" });
        shipResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CancelShippingOrder_Works()
    {
        var client = await AuthenticatedClient();
        var warehouseId = await SeedWarehouse();
        var productId = await SeedProduct();

        var createResponse = await client.PostAsJsonAsync("/api/v1/warehouse/shipping", new
        {
            warehouseId,
            orderType = "Manual",
            lines = new[] { new { productId, orderedQty = 10m } }
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = created.GetProperty("id").GetString()!;

        var response = await client.PostAsync($"/api/v1/warehouse/shipping/{orderId}/cancel", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
