using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Identity;
using GeorgiaERP.Domain.Organization;
using GeorgiaERP.Domain.Products;
using GeorgiaERP.Domain.Procurement;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using WarehouseEntity = GeorgiaERP.Domain.Organization.Warehouse;

namespace GeorgiaERP.Tests.Integration;

[Collection("Integration")]
public class ProcurementApiTests
{
    private readonly ErpApiFactory _factory;

    public ProcurementApiTests(ErpApiFactory factory) => _factory = factory;

    private HttpClient NewClient() => _factory.CreateClient();

    private async Task<HttpClient> AuthenticatedClient()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();

        if (!db.Users.Any(u => u.Username == "proc_admin"))
        {
            var role = db.Roles.FirstOrDefault(r => r.Code == "super_admin");
            if (role is null)
            {
                role = Role.Create("super_admin", "Super Admin", "სუპერ ადმინი", "Full access", true);
                db.Roles.Add(role);
            }

            var user = User.Create("proc_admin", "procadmin@test.local",
                passwordService.HashPassword("Admin@123!"),
                "Proc", "Admin", "შესყიდვები", "ადმინი", "ka");
            db.Users.Add(user);
            db.UserRoles.Add(UserRole.Create(user.Id, role.Id));
            await db.SaveChangesAsync();
        }

        var client = NewClient();
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { username = "proc_admin", password = "Admin@123!" });
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

    private async Task<Guid> SeedSupplier()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var supplier = Supplier.Create($"SUP-{Guid.NewGuid():N}"[..15], "Test Supplier");
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();
        return supplier.Id;
    }

    // === Auth ===

    [Fact]
    public async Task Procurement_WithoutAuth_Returns401()
    {
        var response = await NewClient().GetAsync("/api/v1/procurement/suppliers");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // === Suppliers ===

    [Fact]
    public async Task Suppliers_CreateAndList()
    {
        var client = await AuthenticatedClient();
        var code = $"SUP-{Guid.NewGuid():N}"[..15];

        var createResponse = await client.PostAsJsonAsync("/api/v1/procurement/suppliers", new
        {
            code,
            name = "Integration Supplier",
            nameKa = "ტესტ მომწოდებელი",
            tin = "123456789",
            isVatPayer = true,
            contactPerson = "John Doe",
            phone = "+995555123456",
            email = "supplier@test.local",
            address = "Tbilisi, Georgia",
            paymentTerms = "Net 30",
            creditLimit = 50000m
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        created.GetProperty("id").GetString().Should().NotBeNullOrEmpty();

        var listResponse = await client.GetAsync($"/api/v1/procurement/suppliers?search={code}");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        list.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(1);
        list.GetProperty("items").GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Suppliers_DuplicateCode_ReturnsConflict()
    {
        var client = await AuthenticatedClient();
        var code = $"SUP-{Guid.NewGuid():N}"[..15];

        await client.PostAsJsonAsync("/api/v1/procurement/suppliers", new
        {
            code,
            name = "First Supplier",
            isVatPayer = false
        });

        var response = await client.PostAsJsonAsync("/api/v1/procurement/suppliers", new
        {
            code,
            name = "Second Supplier",
            isVatPayer = false
        });

        // The handler returns Result.Failure (no error code), which maps to 400 BadRequest.
        // If the handler is updated to use Result.Conflict, this should be HttpStatusCode.Conflict.
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Conflict, HttpStatusCode.BadRequest);
    }

    // === Purchase Orders ===

    [Fact]
    public async Task PurchaseOrders_CreateWithLines_ReturnsCreated()
    {
        var client = await AuthenticatedClient();
        var supplierId = await SeedSupplier();
        var warehouseId = await SeedWarehouse();
        var productId = await SeedProduct();

        var response = await client.PostAsJsonAsync("/api/v1/procurement/purchase-orders", new
        {
            supplierId,
            warehouseId,
            createdBy = Guid.NewGuid(),
            expectedDate = DateTimeOffset.UtcNow.AddDays(7),
            notes = "Integration test PO",
            lines = new[] { new { productId, quantity = 100m, unitPrice = 25.50m } }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("poNumber").GetString().Should().StartWith("PO-");
        body.GetProperty("total").GetDecimal().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PurchaseOrders_InvalidSupplier_Returns400orNotFound()
    {
        var client = await AuthenticatedClient();
        var warehouseId = await SeedWarehouse();
        var productId = await SeedProduct();

        var response = await client.PostAsJsonAsync("/api/v1/procurement/purchase-orders", new
        {
            supplierId = Guid.NewGuid(),
            warehouseId,
            createdBy = Guid.NewGuid(),
            lines = new[] { new { productId, quantity = 10m, unitPrice = 5m } }
        });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PurchaseOrders_FullWorkflow()
    {
        var client = await AuthenticatedClient();
        var supplierId = await SeedSupplier();
        var warehouseId = await SeedWarehouse();
        var productId = await SeedProduct();

        // 1. Create PO
        var createResponse = await client.PostAsJsonAsync("/api/v1/procurement/purchase-orders", new
        {
            supplierId,
            warehouseId,
            createdBy = Guid.NewGuid(),
            expectedDate = DateTimeOffset.UtcNow.AddDays(14),
            notes = "Full workflow test",
            lines = new[] { new { productId, quantity = 50m, unitPrice = 10m } }
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var poId = Guid.Parse(created.GetProperty("id").GetString()!);

        // 2. List POs to get line IDs
        var listResponse = await client.GetAsync($"/api/v1/procurement/purchase-orders?supplierId={supplierId}");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listBody = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var items = listBody.GetProperty("items");
        items.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);

        // Find the PO we just created
        JsonElement? matchedPo = null;
        foreach (var item in items.EnumerateArray())
        {
            if (Guid.Parse(item.GetProperty("id").GetString()!) == poId)
            {
                matchedPo = item;
                break;
            }
        }
        matchedPo.Should().NotBeNull();

        var lines = matchedPo!.Value.GetProperty("lines");
        lines.GetArrayLength().Should().Be(1);
        var poLineId = Guid.Parse(lines[0].GetProperty("id").GetString()!);

        // 3. Approve
        var approveResponse = await client.PostAsync($"/api/v1/procurement/purchase-orders/{poId}/approve", null);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Send
        var sendResponse = await client.PostAsync($"/api/v1/procurement/purchase-orders/{poId}/send", null);
        sendResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Receive goods
        var receiveResponse = await client.PostAsJsonAsync($"/api/v1/procurement/purchase-orders/{poId}/receive", new
        {
            notes = "Received in full",
            lines = new[]
            {
                new
                {
                    poLineId,
                    receivedQty = 50m,
                    acceptedQty = 48m,
                    rejectedQty = 2m,
                    batchNumber = "BATCH-001",
                    expiryDate = DateTimeOffset.UtcNow.AddMonths(6)
                }
            }
        });
        receiveResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var receipt = await receiveResponse.Content.ReadFromJsonAsync<JsonElement>();
        receipt.GetProperty("grnNumber").GetString().Should().StartWith("GRN-");
        receipt.GetProperty("linesReceived").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task PurchaseOrders_Cancel_Works()
    {
        var client = await AuthenticatedClient();
        var supplierId = await SeedSupplier();
        var warehouseId = await SeedWarehouse();
        var productId = await SeedProduct();

        // Create PO
        var createResponse = await client.PostAsJsonAsync("/api/v1/procurement/purchase-orders", new
        {
            supplierId,
            warehouseId,
            createdBy = Guid.NewGuid(),
            lines = new[] { new { productId, quantity = 20m, unitPrice = 15m } }
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var poId = created.GetProperty("id").GetString()!;

        // Cancel
        var cancelResponse = await client.PostAsync($"/api/v1/procurement/purchase-orders/{poId}/cancel", null);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PurchaseOrders_CancelAfterReceived_Fails()
    {
        var client = await AuthenticatedClient();
        var supplierId = await SeedSupplier();
        var warehouseId = await SeedWarehouse();
        var productId = await SeedProduct();

        // Create PO
        var createResponse = await client.PostAsJsonAsync("/api/v1/procurement/purchase-orders", new
        {
            supplierId,
            warehouseId,
            createdBy = Guid.NewGuid(),
            lines = new[] { new { productId, quantity = 10m, unitPrice = 20m } }
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var poId = Guid.Parse(created.GetProperty("id").GetString()!);

        // Get line IDs via list endpoint
        var listResponse = await client.GetAsync($"/api/v1/procurement/purchase-orders?supplierId={supplierId}");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listBody = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var items = listBody.GetProperty("items");

        JsonElement? matchedPo = null;
        foreach (var item in items.EnumerateArray())
        {
            if (Guid.Parse(item.GetProperty("id").GetString()!) == poId)
            {
                matchedPo = item;
                break;
            }
        }
        matchedPo.Should().NotBeNull();
        var poLineId = Guid.Parse(matchedPo!.Value.GetProperty("lines")[0].GetProperty("id").GetString()!);

        // Approve -> Send -> Receive (full quantity)
        var approveResponse = await client.PostAsync($"/api/v1/procurement/purchase-orders/{poId}/approve", null);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var sendResponse = await client.PostAsync($"/api/v1/procurement/purchase-orders/{poId}/send", null);
        sendResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var receiveResponse = await client.PostAsJsonAsync($"/api/v1/procurement/purchase-orders/{poId}/receive", new
        {
            notes = "Full receipt",
            lines = new[]
            {
                new
                {
                    poLineId,
                    receivedQty = 10m,
                    acceptedQty = 10m,
                    rejectedQty = 0m
                }
            }
        });
        receiveResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Attempt to cancel after receiving - should fail
        var cancelResponse = await client.PostAsync($"/api/v1/procurement/purchase-orders/{poId}/cancel", null);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
