using FluentAssertions;
using GeorgiaERP.Application.Inventory.Commands;
using GeorgiaERP.Domain.Inventory;
using GeorgiaERP.Domain.Organization;
using GeorgiaERP.Domain.Products;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using WarehouseEntity = GeorgiaERP.Domain.Organization.Warehouse;

namespace GeorgiaERP.Tests.Application;

public class InventoryCommandHandlerTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"inventory-{Guid.NewGuid()}")
            .Options);

    private static async Task<(Guid ProductId, Guid WarehouseId)> SeedProductAndWarehouse(AppDbContext db)
    {
        var cat = Category.Create("CAT-INV", "Test");
        db.Categories.Add(cat);
        var product = Product.Create("SKU-INV", "Product", cat.Id, "Piece");
        db.Products.Add(product);
        var warehouse = WarehouseEntity.Create("WH-INV", "Test Warehouse", WarehouseType.Central);
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync();
        return (product.Id, warehouse.Id);
    }

    private static async Task<Guid> SeedStockLevel(AppDbContext db, Guid productId, Guid warehouseId, decimal qty = 100m)
    {
        var stock = StockLevel.Create(productId, warehouseId, 10m);
        stock.AddStock(qty);
        db.StockLevels.Add(stock);
        await db.SaveChangesAsync();
        return stock.Id;
    }

    // === AdjustStock ===

    [Fact]
    public async Task AdjustStock_Positive_AddsStock()
    {
        await using var db = NewContext();
        var (productId, warehouseId) = await SeedProductAndWarehouse(db);
        await SeedStockLevel(db, productId, warehouseId, 50m);

        var handler = new AdjustStockCommandHandler(db);
        var result = await handler.Handle(
            new AdjustStockCommand(productId, warehouseId, 25m, Guid.NewGuid(), "Add stock"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var stock = await db.StockLevels.FirstAsync();
        stock.QuantityOnHand.Should().Be(75m);
        (await db.StockMovements.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task AdjustStock_Negative_DeductsStock()
    {
        await using var db = NewContext();
        var (productId, warehouseId) = await SeedProductAndWarehouse(db);
        await SeedStockLevel(db, productId, warehouseId, 50m);

        var handler = new AdjustStockCommandHandler(db);
        var result = await handler.Handle(
            new AdjustStockCommand(productId, warehouseId, -10m, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var stock = await db.StockLevels.FirstAsync();
        stock.QuantityOnHand.Should().Be(40m);
    }

    [Fact]
    public async Task AdjustStock_Zero_ReturnsFailure()
    {
        await using var db = NewContext();
        var (productId, warehouseId) = await SeedProductAndWarehouse(db);
        await SeedStockLevel(db, productId, warehouseId, 50m);

        var handler = new AdjustStockCommandHandler(db);
        var result = await handler.Handle(
            new AdjustStockCommand(productId, warehouseId, 0m, Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("zero");
    }

    [Fact]
    public async Task AdjustStock_NoStockLevel_ReturnsFailure()
    {
        await using var db = NewContext();
        var handler = new AdjustStockCommandHandler(db);

        var result = await handler.Handle(
            new AdjustStockCommand(Guid.NewGuid(), Guid.NewGuid(), 10m, Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    // === CreateTransferOrder ===

    [Fact]
    public async Task CreateTransfer_Valid_ReturnsSuccess()
    {
        await using var db = NewContext();
        var (productId, srcWhId) = await SeedProductAndWarehouse(db);
        await SeedStockLevel(db, productId, srcWhId, 100m);
        var destWh = WarehouseEntity.Create("WH-DEST", "Dest Warehouse", WarehouseType.Store);
        db.Warehouses.Add(destWh);
        await db.SaveChangesAsync();

        var handler = new CreateTransferOrderCommandHandler(db, Substitute.For<ILogger<CreateTransferOrderCommandHandler>>());
        var result = await handler.Handle(
            new CreateTransferOrderCommand(srcWhId, destWh.Id, Guid.NewGuid(), "Test transfer",
                [new TransferLineInput(productId, 25m)]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().NotBeEmpty();
        (await db.TransferOrders.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateTransfer_SameWarehouse_ReturnsFailure()
    {
        await using var db = NewContext();
        var (productId, warehouseId) = await SeedProductAndWarehouse(db);

        var handler = new CreateTransferOrderCommandHandler(db, Substitute.For<ILogger<CreateTransferOrderCommandHandler>>());
        var result = await handler.Handle(
            new CreateTransferOrderCommand(warehouseId, warehouseId, Guid.NewGuid(), "Same wh",
                [new TransferLineInput(productId, 10m)]),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
