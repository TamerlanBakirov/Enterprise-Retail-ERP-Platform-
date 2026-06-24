using FluentAssertions;
using GeorgiaERP.Application.Inventory.Commands;
using GeorgiaERP.Application.Inventory.Queries;
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

public class GetTransferOrderByIdQueryTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"transfer-byid-{Guid.NewGuid()}")
            .Options);

    private static async Task<(Guid SrcId, Guid DestId, Guid ProductId)> Seed(AppDbContext db)
    {
        var cat = Category.Create("CAT-T", "Test");
        db.Categories.Add(cat);
        var product = Product.Create("SKU-T", "Steel Bolt", cat.Id, "Piece");
        db.Products.Add(product);
        var src = WarehouseEntity.Create("WH-SRC", "Central DC", WarehouseType.Central);
        var dest = WarehouseEntity.Create("WH-DST", "Store 5", WarehouseType.Store);
        db.Warehouses.Add(src);
        db.Warehouses.Add(dest);
        // Transfer creation requires available stock at the source warehouse.
        var stock = StockLevel.Create(product.Id, src.Id, 10m);
        stock.AddStock(100m);
        db.StockLevels.Add(stock);
        await db.SaveChangesAsync();
        return (src.Id, dest.Id, product.Id);
    }

    [Fact]
    public async Task GetById_ReturnsOrderWithNamedWarehousesAndLines()
    {
        await using var db = NewContext();
        var (srcId, destId, productId) = await Seed(db);
        var created = await new CreateTransferOrderCommandHandler(
                db, Substitute.For<ILogger<CreateTransferOrderCommandHandler>>())
            .Handle(new CreateTransferOrderCommand(srcId, destId, Guid.NewGuid(), "Restock",
                [new TransferLineInput(productId, 25m)]), CancellationToken.None);

        var result = await new GetTransferOrderByIdQueryHandler(db)
            .Handle(new GetTransferOrderByIdQuery(created.Value!.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SourceWarehouseName.Should().Be("Central DC");
        result.Value.DestWarehouseName.Should().Be("Store 5");
        result.Value.Lines.Should().HaveCount(1);
        result.Value.Lines[0].ProductName.Should().Be("Steel Bolt");
        result.Value.Lines[0].RequestedQty.Should().Be(25m);
    }

    [Fact]
    public async Task GetById_Unknown_ReturnsNotFound()
    {
        await using var db = NewContext();

        var result = await new GetTransferOrderByIdQueryHandler(db)
            .Handle(new GetTransferOrderByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }
}
