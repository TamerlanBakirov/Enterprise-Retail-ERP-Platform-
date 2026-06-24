using FluentAssertions;
using GeorgiaERP.Application.Procurement.Commands;
using GeorgiaERP.Application.Procurement.Queries;
using GeorgiaERP.Domain.Organization;
using GeorgiaERP.Domain.Procurement;
using GeorgiaERP.Domain.Products;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
using WarehouseEntity = GeorgiaERP.Domain.Organization.Warehouse;

namespace GeorgiaERP.Tests.Application;

public class GetPurchaseOrderByIdQueryTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"po-byid-{Guid.NewGuid()}")
            .Options);

    private static async Task<(Guid SupplierId, Guid WarehouseId, Guid ProductId)> SeedBaseData(AppDbContext db)
    {
        var supplier = Supplier.Create("SUP-001", "Acme Supply");
        db.Suppliers.Add(supplier);
        var warehouse = WarehouseEntity.Create("WH-1", "Central", WarehouseType.Central);
        db.Warehouses.Add(warehouse);
        var cat = Category.Create("CAT-1", "Tools");
        db.Categories.Add(cat);
        var product = Product.Create("SKU-1", "Cordless Drill", cat.Id, "Piece");
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return (supplier.Id, warehouse.Id, product.Id);
    }

    [Fact]
    public async Task GetById_ReturnsOrderWithSupplierAndNamedLines()
    {
        await using var db = NewContext();
        var (suppId, whId, prodId) = await SeedBaseData(db);
        var created = await new CreatePurchaseOrderCommandHandler(db).Handle(
            new CreatePurchaseOrderCommand(suppId, whId, Guid.NewGuid(),
                DateTimeOffset.UtcNow.AddDays(7), "PO notes",
                [new PoLineInput(prodId, 10m, 25m)]),
            CancellationToken.None);

        var result = await new GetPurchaseOrderByIdQueryHandler(db)
            .Handle(new GetPurchaseOrderByIdQuery(created.Value!.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SupplierName.Should().Be("Acme Supply");
        result.Value.Lines.Should().HaveCount(1);
        result.Value.Lines[0].ProductName.Should().Be("Cordless Drill"); // resolved, not null
        result.Value.Lines[0].OrderedQty.Should().Be(10m);
    }

    [Fact]
    public async Task GetById_Unknown_ReturnsNotFound()
    {
        await using var db = NewContext();

        var result = await new GetPurchaseOrderByIdQueryHandler(db)
            .Handle(new GetPurchaseOrderByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }
}
