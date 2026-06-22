using FluentAssertions;
using GeorgiaERP.Application.Procurement.Commands;
using GeorgiaERP.Domain.Organization;
using GeorgiaERP.Domain.Procurement;
using GeorgiaERP.Domain.Products;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
using WarehouseEntity = GeorgiaERP.Domain.Organization.Warehouse;

namespace GeorgiaERP.Tests.Application;

public class ProcurementCommandHandlerTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"procurement-{Guid.NewGuid()}")
            .Options);

    private static async Task<(Guid SupplierId, Guid WarehouseId, Guid ProductId)> SeedBaseData(AppDbContext db)
    {
        var supplier = Supplier.Create("SUP-001", "Test Supplier");
        db.Suppliers.Add(supplier);
        var warehouse = WarehouseEntity.Create("WH-PROC", "Test Warehouse", WarehouseType.Central);
        db.Warehouses.Add(warehouse);
        var cat = Category.Create("CAT-PROC", "Test");
        db.Categories.Add(cat);
        var product = Product.Create("SKU-PROC", "Product", cat.Id, "Piece");
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return (supplier.Id, warehouse.Id, product.Id);
    }

    // === CreatePurchaseOrder ===

    [Fact]
    public async Task CreatePO_Valid_ReturnsSuccess()
    {
        await using var db = NewContext();
        var (suppId, whId, prodId) = await SeedBaseData(db);
        var handler = new CreatePurchaseOrderCommandHandler(db);

        var result = await handler.Handle(new CreatePurchaseOrderCommand(
            suppId, whId, Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddDays(7),
            "Test PO",
            [new PoLineInput(prodId, 100m, 5.50m)]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PoNumber.Should().StartWith("PO-");
        result.Value.Total.Should().BeGreaterThan(0);
        (await db.PurchaseOrders.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreatePO_InvalidSupplier_ReturnsFailure()
    {
        await using var db = NewContext();
        var (_, whId, prodId) = await SeedBaseData(db);
        var handler = new CreatePurchaseOrderCommandHandler(db);

        var result = await handler.Handle(new CreatePurchaseOrderCommand(
            Guid.NewGuid(), whId, Guid.NewGuid(), null, null,
            [new PoLineInput(prodId, 10m, 5m)]),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Supplier");
    }

    [Fact]
    public async Task CreatePO_InvalidWarehouse_ReturnsFailure()
    {
        await using var db = NewContext();
        var (suppId, _, prodId) = await SeedBaseData(db);
        var handler = new CreatePurchaseOrderCommandHandler(db);

        var result = await handler.Handle(new CreatePurchaseOrderCommand(
            suppId, Guid.NewGuid(), Guid.NewGuid(), null, null,
            [new PoLineInput(prodId, 10m, 5m)]),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Warehouse");
    }

    [Fact]
    public async Task CreatePO_InvalidProduct_ReturnsFailure()
    {
        await using var db = NewContext();
        var (suppId, whId, _) = await SeedBaseData(db);
        var handler = new CreatePurchaseOrderCommandHandler(db);

        var result = await handler.Handle(new CreatePurchaseOrderCommand(
            suppId, whId, Guid.NewGuid(), null, null,
            [new PoLineInput(Guid.NewGuid(), 10m, 5m)]),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Product");
    }

    [Fact]
    public async Task CreatePO_CalculatesVatCorrectly()
    {
        await using var db = NewContext();
        var (suppId, whId, prodId) = await SeedBaseData(db);
        var handler = new CreatePurchaseOrderCommandHandler(db);

        var result = await handler.Handle(new CreatePurchaseOrderCommand(
            suppId, whId, Guid.NewGuid(), null, null,
            [new PoLineInput(prodId, 10m, 100m)]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // 10 * 100 = 1000 subtotal, 1000 * 0.18 = 180 VAT, total = 1180
        result.Value!.Total.Should().Be(1180m);
    }

    // === CreateSupplier ===

    [Fact]
    public async Task CreateSupplier_Valid_ReturnsSuccess()
    {
        await using var db = NewContext();
        var handler = new CreateSupplierCommandHandler(db);

        var result = await handler.Handle(new CreateSupplierCommand(
            "SUP-NEW", "New Supplier", "ახალი მომწოდებელი",
            "123456789", true, "John Doe", "+995555111222", "supplier@test.ge",
            "123 Main St, Tbilisi", "NET30", 50000m),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await db.Suppliers.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateSupplier_DuplicateCode_ReturnsFailure()
    {
        await using var db = NewContext();
        db.Suppliers.Add(Supplier.Create("SUP-DUP", "Existing"));
        await db.SaveChangesAsync();

        var handler = new CreateSupplierCommandHandler(db);
        var result = await handler.Handle(new CreateSupplierCommand(
            "SUP-DUP", "Duplicate", null, null, false, null, null, null, null, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }
}
