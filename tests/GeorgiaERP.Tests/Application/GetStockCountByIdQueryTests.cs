using FluentAssertions;
using GeorgiaERP.Application.Inventory.Queries;
using GeorgiaERP.Domain.Inventory;
using GeorgiaERP.Domain.Organization;
using GeorgiaERP.Domain.Products;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
using WarehouseEntity = GeorgiaERP.Domain.Organization.Warehouse;

namespace GeorgiaERP.Tests.Application;

public class GetStockCountByIdQueryTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"count-byid-{Guid.NewGuid()}")
            .Options);

    [Fact]
    public async Task GetById_ReturnsLinesWithVarianceAndRollups()
    {
        await using var db = NewContext();
        var cat = Category.Create("CAT-C", "Test");
        db.Categories.Add(cat);
        var p1 = Product.Create("SKU-A", "Widget A", cat.Id, "Piece");
        var p2 = Product.Create("SKU-B", "Widget B", cat.Id, "Piece");
        db.Products.AddRange(p1, p2);
        var wh = WarehouseEntity.Create("WH-C", "Count House", WarehouseType.Central);
        db.Warehouses.Add(wh);

        var count = StockCount.Create(wh.Id, CountType.Full, Guid.NewGuid());
        var line1 = StockCountLine.Create(count.Id, p1.Id, expectedQty: 100m);
        line1.RecordCount(95m, Guid.NewGuid());   // variance -5
        var line2 = StockCountLine.Create(count.Id, p2.Id, expectedQty: 50m);
        line2.RecordCount(50m, Guid.NewGuid());   // no variance
        count.Lines.Add(line1);
        count.Lines.Add(line2);
        db.StockCounts.Add(count);
        await db.SaveChangesAsync();

        var result = await new GetStockCountByIdQueryHandler(db)
            .Handle(new GetStockCountByIdQuery(count.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.WarehouseName.Should().Be("Count House");
        result.Value.Lines.Should().HaveCount(2);
        result.Value.TotalVariance.Should().Be(-5m);
        result.Value.LinesWithVariance.Should().Be(1);
        result.Value.Lines.Single(l => l.ProductName == "Widget A").Variance.Should().Be(-5m);
    }

    [Fact]
    public async Task GetById_Unknown_ReturnsNotFound()
    {
        await using var db = NewContext();

        var result = await new GetStockCountByIdQueryHandler(db)
            .Handle(new GetStockCountByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }
}
