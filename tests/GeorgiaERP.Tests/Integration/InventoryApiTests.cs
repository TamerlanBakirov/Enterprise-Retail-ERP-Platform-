using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Identity;
using GeorgiaERP.Domain.Inventory;
using GeorgiaERP.Domain.Organization;
using GeorgiaERP.Domain.Products;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using WarehouseEntity = GeorgiaERP.Domain.Organization.Warehouse;

namespace GeorgiaERP.Tests.Integration;

[Collection("Integration")]
public class InventoryApiTests
{
    private readonly ErpApiFactory _factory;

    public InventoryApiTests(ErpApiFactory factory) => _factory = factory;

    private HttpClient NewClient() => _factory.CreateClient();

    private async Task<HttpClient> AuthenticatedClient()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();

        if (!db.Users.Any(u => u.Username == "inv_admin"))
        {
            var role = db.Roles.FirstOrDefault(r => r.Code == "super_admin");
            if (role is null)
            {
                role = Role.Create("super_admin", "Super Admin", "სუპერ ადმინი", "Full access", true);
                db.Roles.Add(role);
            }

            var user = User.Create("inv_admin", "invadmin@test.local",
                passwordService.HashPassword("Admin@123!"),
                "Inv", "Admin", "ინვენტარი", "ადმინი", "ka");
            db.Users.Add(user);
            db.UserRoles.Add(UserRole.Create(user.Id, role.Id));
            await db.SaveChangesAsync();
        }

        var client = NewClient();
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { username = "inv_admin", password = "Admin@123!" });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<Guid> SeedWarehouse(string? suffix = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var code = $"WH-{suffix ?? Guid.NewGuid().ToString("N")}"[..15];
        var wh = WarehouseEntity.Create(code, "Test Warehouse", WarehouseType.Central);
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

    private async Task<Guid> SeedStockLevel(Guid productId, Guid warehouseId, decimal initialQty = 100m)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stock = StockLevel.Create(productId, warehouseId, 10.00m);
        stock.AddStock(initialQty);
        db.StockLevels.Add(stock);
        await db.SaveChangesAsync();
        return stock.Id;
    }

    // === Auth ===

    [Fact]
    public async Task Inventory_WithoutAuth_Returns401()
    {
        var response = await NewClient().GetAsync("/api/v1/inventory/stock-levels");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // === Stock Levels ===

    [Fact]
    public async Task StockLevels_List_ReturnsOk()
    {
        var client = await AuthenticatedClient();
        var warehouseId = await SeedWarehouse();
        var productId = await SeedProduct();
        await SeedStockLevel(productId, warehouseId);

        var response = await client.GetAsync("/api/v1/inventory/stock-levels?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().NotBe(JsonValueKind.Undefined);
    }

    [Fact]
    public async Task StockLevels_FilterByWarehouse_Works()
    {
        var client = await AuthenticatedClient();
        var warehouseId = await SeedWarehouse();
        var productId = await SeedProduct();
        await SeedStockLevel(productId, warehouseId);

        var response = await client.GetAsync($"/api/v1/inventory/stock-levels?warehouseId={warehouseId}&page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().NotBe(JsonValueKind.Undefined);
    }

    // === Stock Movements ===

    [Fact]
    public async Task StockMovements_List_ReturnsOk()
    {
        var client = await AuthenticatedClient();

        var response = await client.GetAsync("/api/v1/inventory/movements?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // === Stock Adjustment ===

    [Fact]
    public async Task AdjustStock_Positive_ReturnsOk()
    {
        var client = await AuthenticatedClient();
        var warehouseId = await SeedWarehouse();
        var productId = await SeedProduct();
        await SeedStockLevel(productId, warehouseId, 50m);

        var response = await client.PostAsJsonAsync("/api/v1/inventory/adjust", new
        {
            productId,
            warehouseId,
            quantity = 10m,
            adjustedBy = Guid.NewGuid(),
            notes = "Test positive adjustment"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AdjustStock_NoStockLevel_ReturnsBadRequest()
    {
        var client = await AuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/v1/inventory/adjust", new
        {
            productId = Guid.NewGuid(),
            warehouseId = Guid.NewGuid(),
            quantity = 10m,
            adjustedBy = Guid.NewGuid(),
            notes = "Should fail - no stock level"
        });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    // === Transfer Orders ===

    [Fact]
    public async Task TransferOrders_Create_ReturnsCreated()
    {
        var client = await AuthenticatedClient();
        var sourceWarehouseId = await SeedWarehouse();
        var destWarehouseId = await SeedWarehouse();
        var productId = await SeedProduct();
        await SeedStockLevel(productId, sourceWarehouseId, 100m);

        var response = await client.PostAsJsonAsync("/api/v1/inventory/transfers", new
        {
            sourceWarehouseId,
            destWarehouseId,
            requestedBy = Guid.NewGuid(),
            notes = "Test transfer",
            lines = new[] { new { productId, quantity = 10m } }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task TransferOrders_SameWarehouse_ReturnsError()
    {
        var client = await AuthenticatedClient();
        var warehouseId = await SeedWarehouse();
        var productId = await SeedProduct();
        await SeedStockLevel(productId, warehouseId, 100m);

        var response = await client.PostAsJsonAsync("/api/v1/inventory/transfers", new
        {
            sourceWarehouseId = warehouseId,
            destWarehouseId = warehouseId,
            requestedBy = Guid.NewGuid(),
            notes = "Same warehouse transfer",
            lines = new[] { new { productId, quantity = 10m } }
        });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Conflict,
            HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task TransferOrders_FullWorkflow()
    {
        var client = await AuthenticatedClient();
        var sourceWarehouseId = await SeedWarehouse();
        var destWarehouseId = await SeedWarehouse();
        var productId = await SeedProduct();
        await SeedStockLevel(productId, sourceWarehouseId, 200m);

        // Create
        var createResponse = await client.PostAsJsonAsync("/api/v1/inventory/transfers", new
        {
            sourceWarehouseId,
            destWarehouseId,
            requestedBy = Guid.NewGuid(),
            notes = "Full workflow transfer",
            lines = new[] { new { productId, quantity = 25m } }
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var transferId = created.GetProperty("id").GetString()!;

        // Approve
        var approveResponse = await client.PostAsync($"/api/v1/inventory/transfers/{transferId}/approve", null);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Ship
        var shipResponse = await client.PostAsync($"/api/v1/inventory/transfers/{transferId}/ship", null);
        shipResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Receive
        var receiveResponse = await client.PostAsync($"/api/v1/inventory/transfers/{transferId}/receive", null);
        receiveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TransferOrders_Cancel_Works()
    {
        var client = await AuthenticatedClient();
        var sourceWarehouseId = await SeedWarehouse();
        var destWarehouseId = await SeedWarehouse();
        var productId = await SeedProduct();
        await SeedStockLevel(productId, sourceWarehouseId, 100m);

        // Create
        var createResponse = await client.PostAsJsonAsync("/api/v1/inventory/transfers", new
        {
            sourceWarehouseId,
            destWarehouseId,
            requestedBy = Guid.NewGuid(),
            notes = "Transfer to cancel",
            lines = new[] { new { productId, quantity = 5m } }
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var transferId = created.GetProperty("id").GetString()!;

        // Cancel
        var cancelResponse = await client.PostAsync($"/api/v1/inventory/transfers/{transferId}/cancel", null);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // === Stock Counts ===

    [Fact]
    public async Task StockCounts_Create_ReturnsCreated()
    {
        var client = await AuthenticatedClient();
        var warehouseId = await SeedWarehouse();
        var productId = await SeedProduct();
        await SeedStockLevel(productId, warehouseId, 50m);

        var response = await client.PostAsJsonAsync("/api/v1/inventory/counts", new
        {
            warehouseId,
            countType = "Cycle",
            createdBy = Guid.NewGuid(),
            productIds = new[] { productId }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task StockCounts_ListPaged_ReturnsOk()
    {
        var client = await AuthenticatedClient();

        var response = await client.GetAsync("/api/v1/inventory/counts?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
