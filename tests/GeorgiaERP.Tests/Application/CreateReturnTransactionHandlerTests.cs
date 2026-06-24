using FluentAssertions;
using GeorgiaERP.Application.Compliance;
using GeorgiaERP.Application.POS.Commands;
using GeorgiaERP.Domain.Inventory;
using GeorgiaERP.Domain.Organization;
using GeorgiaERP.Domain.POS;
using GeorgiaERP.Domain.Products;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using WarehouseEntity = GeorgiaERP.Domain.Organization.Warehouse;

namespace GeorgiaERP.Tests.Application;

public class CreateReturnTransactionHandlerTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"pos-return-{Guid.NewGuid()}")
            .Options);

    private sealed record Fixture(Guid SessionId, Guid ProductId, Guid WarehouseId, decimal StartStock);

    private static async Task<Fixture> Seed(AppDbContext db, decimal stockQty = 100m)
    {
        var store = Store.Create("STR-R", "Return Store", StoreType.Retail);
        db.Stores.Add(store);
        var terminal = PosTerminal.Create("TERM-R", store.Id, "Reg", TerminalType.Register);
        db.PosTerminals.Add(terminal);
        var session = PosSession.Create(terminal.Id, Guid.NewGuid(), 500m);
        db.PosSessions.Add(session);
        var cat = Category.Create("CAT-R", "Cat");
        db.Categories.Add(cat);
        var product = Product.Create("SKU-R", "Returnable", cat.Id, "Piece");
        db.Products.Add(product);
        var wh = WarehouseEntity.Create("WH-R", "WH", WarehouseType.Central);
        wh.LinkToStore(store.Id);
        db.Warehouses.Add(wh);
        var stock = StockLevel.Create(product.Id, wh.Id, 10m);
        stock.AddStock(stockQty);
        db.StockLevels.Add(stock);
        await db.SaveChangesAsync();
        return new Fixture(session.Id, product.Id, wh.Id, stockQty);
    }

    private static CreatePosTransactionCommandHandler SaleHandler(AppDbContext db) =>
        new(db, Substitute.For<IRsGeQueuePublisher>(),
            Substitute.For<ILogger<CreatePosTransactionCommandHandler>>());

    private static CreateReturnTransactionCommandHandler ReturnHandler(AppDbContext db) =>
        new(db, Substitute.For<ILogger<CreateReturnTransactionCommandHandler>>());

    private static async Task<Guid> SellFive(AppDbContext db, Fixture fx)
    {
        var sale = await SaleHandler(db).Handle(new CreatePosTransactionCommand(
            fx.SessionId, null,
            [new PosLineInput(fx.ProductId, null, 5m, 100m)],
            [new PosPaymentInput(PaymentMethod.Cash, 500m)]), CancellationToken.None);
        sale.IsSuccess.Should().BeTrue();
        return sale.Value!.TransactionId;
    }

    [Fact]
    public async Task Return_PartialSale_RestocksAndComputesRefund()
    {
        await using var db = NewContext();
        var fx = await Seed(db);
        var saleId = await SellFive(db, fx);

        var result = await ReturnHandler(db).Handle(new CreateReturnTransactionCommand(
            fx.SessionId, saleId, [new ReturnLineInput(fx.ProductId, null, 2m)], "Damaged"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(200m);          // 2 x 100
        result.Value.VatTotal.Should().Be(30.51m);      // prorated VAT of the sale line

        var ret = await db.PosTransactions.SingleAsync(t => t.TransactionType == PosTransactionType.Return);
        ret.OriginalTransactionId.Should().Be(saleId);

        // stock: 100 - 5 sold + 2 returned = 97
        var stock = await db.StockLevels.SingleAsync(s => s.ProductId == fx.ProductId);
        stock.QuantityOnHand.Should().Be(97m);
    }

    [Fact]
    public async Task Return_MoreThanSold_IsRejected()
    {
        await using var db = NewContext();
        var fx = await Seed(db);
        var saleId = await SellFive(db, fx);

        var result = await ReturnHandler(db).Handle(new CreateReturnTransactionCommand(
            fx.SessionId, saleId, [new ReturnLineInput(fx.ProductId, null, 6m)], null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("returnable");
    }

    [Fact]
    public async Task Return_SecondReturn_RespectsAlreadyReturned()
    {
        await using var db = NewContext();
        var fx = await Seed(db);
        var saleId = await SellFive(db, fx);
        var handler = ReturnHandler(db);
        await handler.Handle(new CreateReturnTransactionCommand(
            fx.SessionId, saleId, [new ReturnLineInput(fx.ProductId, null, 3m)], null), CancellationToken.None);

        // 3 already returned, only 2 remain; requesting 3 must fail.
        var second = await handler.Handle(new CreateReturnTransactionCommand(
            fx.SessionId, saleId, [new ReturnLineInput(fx.ProductId, null, 3m)], null), CancellationToken.None);

        second.IsFailure.Should().BeTrue();
        second.Error.Should().Contain("returnable");
    }

    [Fact]
    public async Task Return_UnknownOriginal_ReturnsNotFound()
    {
        await using var db = NewContext();
        var fx = await Seed(db);

        var result = await ReturnHandler(db).Handle(new CreateReturnTransactionCommand(
            fx.SessionId, Guid.NewGuid(), [new ReturnLineInput(fx.ProductId, null, 1m)], null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }
}
