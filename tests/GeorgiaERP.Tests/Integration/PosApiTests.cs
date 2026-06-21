using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Identity;
using GeorgiaERP.Domain.Inventory;
using GeorgiaERP.Domain.Organization;
using GeorgiaERP.Domain.POS;
using GeorgiaERP.Domain.Products;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GeorgiaERP.Tests.Integration;

[Collection("Integration")]
public class PosApiTests : IntegrationTestBase
{
    public PosApiTests(ErpApiFactory factory) : base(factory) { }

    private Task<HttpClient> AuthenticatedClient()
        => AuthenticatedClient("pos_admin", "posadmin@test.local", "POS", "Admin", "პოს");

    /// <summary>
    /// Seeds a Store, linked Warehouse, POS Terminal, Product with stock -- everything
    /// needed for a full POS session + transaction lifecycle.
    /// </summary>
    private async Task<PosTestSeed> SeedPosInfrastructure()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();

        // Ensure admin user exists (cashier)
        var cashier = db.Users.FirstOrDefault(u => u.Username == "pos_admin");
        if (cashier is null)
        {
            var role = db.Roles.FirstOrDefault(r => r.Code == "super_admin");
            if (role is null)
            {
                role = Role.Create("super_admin", "Super Admin", "სუპერ ადმინი", "Full access", true);
                db.Roles.Add(role);
            }

            cashier = User.Create("pos_admin", "posadmin@test.local",
                passwordService.HashPassword("Admin@123!"),
                "POS", "Admin", "პოს", "ადმინი", "ka");
            db.Users.Add(cashier);
            db.UserRoles.Add(UserRole.Create(cashier.Id, role.Id));
            await db.SaveChangesAsync();
        }

        // Store
        var storeCode = $"ST-{Guid.NewGuid():N}"[..10];
        var store = Store.Create(storeCode, "Test POS Store", StoreType.Retail, "ტესტ მაღაზია");
        db.Stores.Add(store);

        // Warehouse linked to the store
        var whCode = $"WH-{Guid.NewGuid():N}"[..10];
        var warehouse = Warehouse.Create(whCode, "Store Warehouse", WarehouseType.Store, "საწყობი");
        warehouse.LinkToStore(store.Id);
        db.Warehouses.Add(warehouse);

        // POS Terminal
        var termCode = $"T-{Guid.NewGuid():N}"[..10];
        var terminal = PosTerminal.Create(termCode, store.Id, "Register 1", TerminalType.Register);
        db.PosTerminals.Add(terminal);

        // Category + Product
        var catCode = $"CAT-{Guid.NewGuid():N}"[..10];
        var category = Category.Create(catCode, "POS Test Category");
        db.Categories.Add(category);

        var sku = $"POS-{Guid.NewGuid():N}"[..15];
        var product = Product.Create(sku, "POS Test Product", category.Id, "PCS");
        db.Products.Add(product);

        // Stock level for product in the warehouse (100 units at 5.00 cost)
        var stockLevel = StockLevel.Create(product.Id, warehouse.Id, 5.00m);
        stockLevel.AddStock(100);
        db.StockLevels.Add(stockLevel);

        await db.SaveChangesAsync();

        return new PosTestSeed(store.Id, warehouse.Id, terminal.Id, cashier.Id, product.Id, sku);
    }

    private record PosTestSeed(
        Guid StoreId, Guid WarehouseId, Guid TerminalId,
        Guid CashierId, Guid ProductId, string Sku);

    // === Auth Guard ===

    [Fact]
    public async Task Pos_Sessions_WithoutAuth_Returns401()
    {
        var response = await NewClient().GetAsync("/api/v1/pos/sessions?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Pos_Transactions_WithoutAuth_Returns401()
    {
        var response = await NewClient().GetAsync("/api/v1/pos/transactions?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // === Session Lifecycle ===

    [Fact]
    public async Task OpenSession_WithValidTerminal_ReturnsOk()
    {
        var client = await AuthenticatedClient();
        var seed = await SeedPosInfrastructure();

        var response = await client.PostAsJsonAsync("/api/v1/pos/sessions", new
        {
            terminalId = seed.TerminalId,
            cashierId = seed.CashierId,
            openingBalance = 500m
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("sessionId").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("status").GetString().Should().Be("Open");
    }

    [Fact]
    public async Task OpenSession_InvalidTerminal_ReturnsFailure()
    {
        var client = await AuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/v1/pos/sessions", new
        {
            terminalId = Guid.NewGuid(),
            cashierId = Guid.NewGuid(),
            openingBalance = 100m
        });

        // The handler returns Result.Failure which maps to a non-OK response
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Terminal not found");
    }

    [Fact]
    public async Task OpenSession_DuplicateOpen_ReturnsFailure()
    {
        var client = await AuthenticatedClient();
        var seed = await SeedPosInfrastructure();

        // Open first session
        var first = await client.PostAsJsonAsync("/api/v1/pos/sessions", new
        {
            terminalId = seed.TerminalId,
            cashierId = seed.CashierId,
            openingBalance = 500m
        });
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        // Attempt to open a second session on the same terminal
        var second = await client.PostAsJsonAsync("/api/v1/pos/sessions", new
        {
            terminalId = seed.TerminalId,
            cashierId = seed.CashierId,
            openingBalance = 200m
        });

        var body = await second.Content.ReadAsStringAsync();
        body.Should().Contain("already has an open session");
    }

    [Fact]
    public async Task GetSessions_ReturnsPagedResult()
    {
        var client = await AuthenticatedClient();

        // First, test without any data -- empty result should be OK
        var emptyResponse = await client.GetAsync("/api/v1/pos/sessions?page=1&pageSize=10");
        emptyResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            await emptyResponse.Content.ReadAsStringAsync());

        // Now seed and open a session
        var seed = await SeedPosInfrastructure();
        await client.PostAsJsonAsync("/api/v1/pos/sessions", new
        {
            terminalId = seed.TerminalId,
            cashierId = seed.CashierId,
            openingBalance = 300m
        });

        var response = await client.GetAsync("/api/v1/pos/sessions?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            await response.Content.ReadAsStringAsync());
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task CloseSession_WithOpenSession_ReturnsOk()
    {
        var client = await AuthenticatedClient();
        var seed = await SeedPosInfrastructure();

        // Open session
        var openResponse = await client.PostAsJsonAsync("/api/v1/pos/sessions", new
        {
            terminalId = seed.TerminalId,
            cashierId = seed.CashierId,
            openingBalance = 500m
        });
        openResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var openBody = await openResponse.Content.ReadFromJsonAsync<JsonElement>();
        var sessionId = openBody.GetProperty("sessionId").GetString()!;

        // Close session
        var closeResponse = await client.PostAsJsonAsync($"/api/v1/pos/sessions/{sessionId}/close", new
        {
            closingBalance = 500m,
            notes = "End of day"
        });

        closeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var closeBody = await closeResponse.Content.ReadFromJsonAsync<JsonElement>();
        closeBody.GetProperty("openingBalance").GetDecimal().Should().Be(500m);
        closeBody.GetProperty("closingBalance").GetDecimal().Should().Be(500m);
    }

    // === Transaction Lifecycle ===

    [Fact]
    public async Task CreateTransaction_WithValidData_ReturnsCreated()
    {
        var client = await AuthenticatedClient();
        var seed = await SeedPosInfrastructure();

        // Open session
        var openResponse = await client.PostAsJsonAsync("/api/v1/pos/sessions", new
        {
            terminalId = seed.TerminalId,
            cashierId = seed.CashierId,
            openingBalance = 500m
        });
        openResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var openBody = await openResponse.Content.ReadFromJsonAsync<JsonElement>();
        var sessionId = Guid.Parse(openBody.GetProperty("sessionId").GetString()!);

        // Create transaction
        var txResponse = await client.PostAsJsonAsync("/api/v1/pos/transactions", new
        {
            sessionId,
            customerId = (Guid?)null,
            lines = new[]
            {
                new
                {
                    productId = seed.ProductId,
                    barcode = (string?)null,
                    quantity = 2m,
                    unitPrice = 10m,
                    discountAmount = 0m,
                    discountReason = (string?)null
                }
            },
            payments = new[]
            {
                new
                {
                    paymentMethod = 0, // Cash
                    amount = 20m,
                    reference = (string?)null,
                    terminalRef = (string?)null
                }
            }
        });

        txResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var txBody = await txResponse.Content.ReadFromJsonAsync<JsonElement>();
        txBody.GetProperty("transactionNumber").GetString().Should().StartWith("TX-");
        txBody.GetProperty("total").GetDecimal().Should().Be(20m);
        txBody.GetProperty("status").GetString().Should().Be("Completed");
        txBody.GetProperty("fiscalDocumentId").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateTransaction_InsufficientStock_ReturnsFailure()
    {
        var client = await AuthenticatedClient();
        var seed = await SeedPosInfrastructure();

        // Open session
        var openResponse = await client.PostAsJsonAsync("/api/v1/pos/sessions", new
        {
            terminalId = seed.TerminalId,
            cashierId = seed.CashierId,
            openingBalance = 500m
        });
        var openBody = await openResponse.Content.ReadFromJsonAsync<JsonElement>();
        var sessionId = Guid.Parse(openBody.GetProperty("sessionId").GetString()!);

        // Try to buy more than available (stock is 100)
        var txResponse = await client.PostAsJsonAsync("/api/v1/pos/transactions", new
        {
            sessionId,
            lines = new[]
            {
                new
                {
                    productId = seed.ProductId,
                    quantity = 999m,
                    unitPrice = 10m,
                    discountAmount = 0m
                }
            },
            payments = new[]
            {
                new { paymentMethod = 0, amount = 9990m }
            }
        });

        var body = await txResponse.Content.ReadAsStringAsync();
        body.Should().Contain("Insufficient stock");
    }

    [Fact]
    public async Task CreateTransaction_InsufficientPayment_ReturnsFailure()
    {
        var client = await AuthenticatedClient();
        var seed = await SeedPosInfrastructure();

        // Open session
        var openResponse = await client.PostAsJsonAsync("/api/v1/pos/sessions", new
        {
            terminalId = seed.TerminalId,
            cashierId = seed.CashierId,
            openingBalance = 500m
        });
        var openBody = await openResponse.Content.ReadFromJsonAsync<JsonElement>();
        var sessionId = Guid.Parse(openBody.GetProperty("sessionId").GetString()!);

        // Pay less than total
        var txResponse = await client.PostAsJsonAsync("/api/v1/pos/transactions", new
        {
            sessionId,
            lines = new[]
            {
                new
                {
                    productId = seed.ProductId,
                    quantity = 5m,
                    unitPrice = 10m,
                    discountAmount = 0m
                }
            },
            payments = new[]
            {
                new { paymentMethod = 0, amount = 1m } // Paying only 1 GEL for 50 GEL
            }
        });

        var body = await txResponse.Content.ReadAsStringAsync();
        body.Should().Contain("Payment total");
    }

    [Fact]
    public async Task GetTransactions_ReturnsPagedResult()
    {
        var client = await AuthenticatedClient();

        var response = await client.GetAsync("/api/v1/pos/transactions?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetTransactionDetail_WithValidId_ReturnsOk()
    {
        var client = await AuthenticatedClient();
        var seed = await SeedPosInfrastructure();

        // Open session + create transaction
        var openResponse = await client.PostAsJsonAsync("/api/v1/pos/sessions", new
        {
            terminalId = seed.TerminalId,
            cashierId = seed.CashierId,
            openingBalance = 500m
        });
        var openBody = await openResponse.Content.ReadFromJsonAsync<JsonElement>();
        var sessionId = Guid.Parse(openBody.GetProperty("sessionId").GetString()!);

        var txResponse = await client.PostAsJsonAsync("/api/v1/pos/transactions", new
        {
            sessionId,
            lines = new[]
            {
                new { productId = seed.ProductId, quantity = 1m, unitPrice = 15m, discountAmount = 0m }
            },
            payments = new[]
            {
                new { paymentMethod = 0, amount = 15m }
            }
        });
        txResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var txBody = await txResponse.Content.ReadFromJsonAsync<JsonElement>();
        var txId = txBody.GetProperty("transactionId").GetString()!;

        // Get detail
        var detailResponse = await client.GetAsync($"/api/v1/pos/transactions/{txId}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await detailResponse.Content.ReadFromJsonAsync<JsonElement>();
        detail.GetProperty("transactionNumber").GetString().Should().StartWith("TX-");
        detail.GetProperty("lines").GetArrayLength().Should().Be(1);
        detail.GetProperty("payments").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task GetTransactionDetail_NotFound_ReturnsFailure()
    {
        var client = await AuthenticatedClient();

        var response = await client.GetAsync($"/api/v1/pos/transactions/{Guid.NewGuid()}");
        // The handler returns Result.Failure, which the base controller maps to an error response
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task VoidTransaction_WithValidTransaction_ReturnsOk()
    {
        var client = await AuthenticatedClient();
        var seed = await SeedPosInfrastructure();

        // Open session + create transaction
        var openResponse = await client.PostAsJsonAsync("/api/v1/pos/sessions", new
        {
            terminalId = seed.TerminalId,
            cashierId = seed.CashierId,
            openingBalance = 500m
        });
        var openBody = await openResponse.Content.ReadFromJsonAsync<JsonElement>();
        var sessionId = Guid.Parse(openBody.GetProperty("sessionId").GetString()!);

        var txResponse = await client.PostAsJsonAsync("/api/v1/pos/transactions", new
        {
            sessionId,
            lines = new[]
            {
                new { productId = seed.ProductId, quantity = 3m, unitPrice = 10m, discountAmount = 0m }
            },
            payments = new[]
            {
                new { paymentMethod = 0, amount = 30m }
            }
        });
        txResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var txBody = await txResponse.Content.ReadFromJsonAsync<JsonElement>();
        var txId = txBody.GetProperty("transactionId").GetString()!;

        // Void the transaction
        var voidResponse = await client.PostAsJsonAsync($"/api/v1/pos/transactions/{txId}/void", new
        {
            reason = "Customer changed mind"
        });

        voidResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task VoidTransaction_AlreadyVoided_ReturnsFailure()
    {
        var client = await AuthenticatedClient();
        var seed = await SeedPosInfrastructure();

        // Open session + create + void
        var openResponse = await client.PostAsJsonAsync("/api/v1/pos/sessions", new
        {
            terminalId = seed.TerminalId,
            cashierId = seed.CashierId,
            openingBalance = 500m
        });
        var openBody = await openResponse.Content.ReadFromJsonAsync<JsonElement>();
        var sessionId = Guid.Parse(openBody.GetProperty("sessionId").GetString()!);

        var txResponse = await client.PostAsJsonAsync("/api/v1/pos/transactions", new
        {
            sessionId,
            lines = new[]
            {
                new { productId = seed.ProductId, quantity = 1m, unitPrice = 20m, discountAmount = 0m }
            },
            payments = new[]
            {
                new { paymentMethod = 0, amount = 20m }
            }
        });
        var txBody = await txResponse.Content.ReadFromJsonAsync<JsonElement>();
        var txId = txBody.GetProperty("transactionId").GetString()!;

        // First void
        await client.PostAsJsonAsync($"/api/v1/pos/transactions/{txId}/void", new
        {
            reason = "Void test"
        });

        // Second void should fail
        var secondVoid = await client.PostAsJsonAsync($"/api/v1/pos/transactions/{txId}/void", new
        {
            reason = "Double void"
        });

        var body = await secondVoid.Content.ReadAsStringAsync();
        body.Should().Contain("already voided");
    }

    [Fact]
    public async Task VoidTransaction_NotFound_ReturnsFailure()
    {
        var client = await AuthenticatedClient();

        var response = await client.PostAsJsonAsync($"/api/v1/pos/transactions/{Guid.NewGuid()}/void", new
        {
            reason = "Test"
        });

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("not found");
    }

    // === Full POS Lifecycle (session open -> transaction -> close) ===

    [Fact]
    public async Task FullPosLifecycle_OpenSession_CreateTransaction_CloseSession()
    {
        var client = await AuthenticatedClient();
        var seed = await SeedPosInfrastructure();

        // 1. Open session
        var openResponse = await client.PostAsJsonAsync("/api/v1/pos/sessions", new
        {
            terminalId = seed.TerminalId,
            cashierId = seed.CashierId,
            openingBalance = 1000m
        });
        openResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var openBody = await openResponse.Content.ReadFromJsonAsync<JsonElement>();
        var sessionId = openBody.GetProperty("sessionId").GetString()!;

        // 2. Create a cash transaction
        var txResponse = await client.PostAsJsonAsync("/api/v1/pos/transactions", new
        {
            sessionId = Guid.Parse(sessionId),
            lines = new[]
            {
                new { productId = seed.ProductId, quantity = 5m, unitPrice = 10m, discountAmount = 0m }
            },
            payments = new[]
            {
                new { paymentMethod = 0, amount = 50m }
            }
        });
        txResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // 3. Close session -- expected balance = opening + cash sales = 1000 + 50 = 1050
        var closeResponse = await client.PostAsJsonAsync($"/api/v1/pos/sessions/{sessionId}/close", new
        {
            closingBalance = 1050m,
            notes = "Balanced close"
        });
        closeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var closeBody = await closeResponse.Content.ReadFromJsonAsync<JsonElement>();
        closeBody.GetProperty("transactionCount").GetInt32().Should().Be(1);
        closeBody.GetProperty("totalSales").GetDecimal().Should().Be(50m);
    }
}
