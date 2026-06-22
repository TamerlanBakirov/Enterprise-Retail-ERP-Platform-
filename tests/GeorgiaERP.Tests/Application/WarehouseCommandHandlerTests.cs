using FluentAssertions;
using GeorgiaERP.Application.Warehouse.Commands;
using GeorgiaERP.Domain.Organization;
using GeorgiaERP.Domain.Warehouse;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
using WarehouseEntity = GeorgiaERP.Domain.Organization.Warehouse;

namespace GeorgiaERP.Tests.Application;

public class WarehouseCommandHandlerTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"warehouse-{Guid.NewGuid()}")
            .Options);

    private static async Task<Guid> SeedWarehouse(AppDbContext db, string code = "WH-001")
    {
        var wh = WarehouseEntity.Create(code, "Test Warehouse", WarehouseType.Central);
        db.Warehouses.Add(wh);
        await db.SaveChangesAsync();
        return wh.Id;
    }

    private static async Task<Guid> SeedProduct(AppDbContext db)
    {
        var cat = GeorgiaERP.Domain.Products.Category.Create("CAT-WH", "Cat");
        db.Categories.Add(cat);
        var prod = GeorgiaERP.Domain.Products.Product.Create("SKU-WH", "Product", cat.Id, "Piece");
        db.Products.Add(prod);
        await db.SaveChangesAsync();
        return prod.Id;
    }

    // === CreateWarehouse ===

    [Fact]
    public async Task CreateWarehouse_Valid_ReturnsSuccess()
    {
        await using var db = NewContext();
        var handler = new CreateWarehouseCommandHandler(db);

        var result = await handler.Handle(new CreateWarehouseCommand(
            "WH-NEW", "New Warehouse", "ახალი საწყობი",
            "Central", "123 Main St", "Tbilisi", "Tbilisi", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await db.Warehouses.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateWarehouse_DuplicateCode_ReturnsFailure()
    {
        await using var db = NewContext();
        await SeedWarehouse(db, "WH-DUP");

        var handler = new CreateWarehouseCommandHandler(db);
        var result = await handler.Handle(new CreateWarehouseCommand(
            "WH-DUP", "Duplicate", null, "Central", null, null, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateWarehouse_InvalidType_ReturnsFailure()
    {
        await using var db = NewContext();
        var handler = new CreateWarehouseCommandHandler(db);

        var result = await handler.Handle(new CreateWarehouseCommand(
            "WH-BAD", "Bad Type", null, "NotAType", null, null, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid warehouse type");
    }

    // === WarehouseLocation ===

    [Fact]
    public async Task CreateLocation_Valid_ReturnsSuccess()
    {
        await using var db = NewContext();
        var whId = await SeedWarehouse(db);

        var handler = new CreateWarehouseLocationCommandHandler(db);
        var result = await handler.Handle(new CreateWarehouseLocationCommand(
            whId, "LOC-A1", "Zone A", "ზონა ა", "Zone", null, 0, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await db.WarehouseLocations.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateLocation_InvalidWarehouse_ReturnsFailure()
    {
        await using var db = NewContext();
        var handler = new CreateWarehouseLocationCommandHandler(db);

        var result = await handler.Handle(new CreateWarehouseLocationCommand(
            Guid.NewGuid(), "LOC-X1", "Zone X", null, "Zone", null, 0, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateLocation_Works()
    {
        await using var db = NewContext();
        var whId = await SeedWarehouse(db);
        var loc = WarehouseLocation.Create(whId, "LOC-D", "Deactivate Me", LocationType.Zone);
        db.WarehouseLocations.Add(loc);
        await db.SaveChangesAsync();

        var handler = new DeactivateWarehouseLocationCommandHandler(db);
        var result = await handler.Handle(
            new DeactivateWarehouseLocationCommand(loc.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var saved = await db.WarehouseLocations.FindAsync(loc.Id);
        saved!.IsActive.Should().BeFalse();
    }

    // === ReceivingOrder ===

    [Fact]
    public async Task CreateReceivingOrder_Valid_ReturnsSuccess()
    {
        await using var db = NewContext();
        var whId = await SeedWarehouse(db);
        var prodId = await SeedProduct(db);

        var handler = new CreateReceivingOrderCommandHandler(db);
        var result = await handler.Handle(new CreateReceivingOrderCommand(
            whId, "PurchaseOrder", null, null, null, null, null,
            [new ReceivingLineInput(prodId, 50m, null)]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await db.ReceivingOrders.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateReceivingOrder_InvalidWarehouse_ReturnsFailure()
    {
        await using var db = NewContext();
        var handler = new CreateReceivingOrderCommandHandler(db);

        var result = await handler.Handle(new CreateReceivingOrderCommand(
            Guid.NewGuid(), "Manual", null, null, null, null, null,
            [new ReceivingLineInput(Guid.NewGuid(), 10m, null)]),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    // === ShippingOrder ===

    [Fact]
    public async Task CreateShippingOrder_Valid_ReturnsSuccess()
    {
        await using var db = NewContext();
        var whId = await SeedWarehouse(db);
        var prodId = await SeedProduct(db);

        var handler = new CreateShippingOrderCommandHandler(db);
        var result = await handler.Handle(new CreateShippingOrderCommand(
            whId, "Manual", null, null, null, null, null, null, null,
            [new ShippingLineInput(prodId, 20m, null)]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await db.ShippingOrders.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateShippingOrder_InvalidWarehouse_ReturnsFailure()
    {
        await using var db = NewContext();
        var handler = new CreateShippingOrderCommandHandler(db);

        var result = await handler.Handle(new CreateShippingOrderCommand(
            Guid.NewGuid(), "Manual", null, null, null, null, null, null, null,
            [new ShippingLineInput(Guid.NewGuid(), 10m, null)]),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
