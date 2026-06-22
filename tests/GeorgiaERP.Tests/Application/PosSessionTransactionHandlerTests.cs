using FluentAssertions;
using GeorgiaERP.Application.Compliance;
using GeorgiaERP.Application.POS.Commands;
using GeorgiaERP.Application.POS.Queries;
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

public class PosSessionTransactionHandlerTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"pos-session-{Guid.NewGuid()}")
            .Options);

    private static async Task<(Guid StoreId, Guid TerminalId)> SeedTerminal(AppDbContext db)
    {
        var store = Store.Create("STR-POS", "POS Store", StoreType.Retail);
        db.Stores.Add(store);
        var terminal = PosTerminal.Create("TERM-001", store.Id, "Register 1", TerminalType.Register);
        db.PosTerminals.Add(terminal);
        await db.SaveChangesAsync();
        return (store.Id, terminal.Id);
    }

    private static async Task<Guid> SeedSession(AppDbContext db, Guid terminalId, decimal openingBalance = 500m)
    {
        var session = PosSession.Create(terminalId, Guid.NewGuid(), openingBalance);
        db.PosSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id;
    }

    private static async Task<(Guid ProductId, Guid WarehouseId)> SeedProductAndStock(
        AppDbContext db, Guid storeId, decimal stockQty = 100m, decimal costPrice = 10m)
    {
        var cat = Category.Create("CAT-POS", "POS Category");
        db.Categories.Add(cat);
        var product = Product.Create("SKU-POS", "Test Product", cat.Id, "Piece");
        db.Products.Add(product);
        var warehouse = WarehouseEntity.Create("WH-POS", "POS Warehouse", WarehouseType.Central);
        warehouse.LinkToStore(storeId);
        db.Warehouses.Add(warehouse);
        var stock = StockLevel.Create(product.Id, warehouse.Id, costPrice);
        stock.AddStock(stockQty);
        db.StockLevels.Add(stock);
        await db.SaveChangesAsync();
        return (product.Id, warehouse.Id);
    }

    // === OpenPosSession ===

    [Fact]
    public async Task OpenSession_Valid_ReturnsSuccess()
    {
        await using var db = NewContext();
        var (_, terminalId) = await SeedTerminal(db);
        var handler = new OpenPosSessionCommandHandler(db);

        var result = await handler.Handle(
            new OpenPosSessionCommand(terminalId, Guid.NewGuid(), 500m),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Open");
        (await db.PosSessions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task OpenSession_InvalidTerminal_ReturnsFailure()
    {
        await using var db = NewContext();
        var handler = new OpenPosSessionCommandHandler(db);

        var result = await handler.Handle(
            new OpenPosSessionCommand(Guid.NewGuid(), Guid.NewGuid(), 500m),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Terminal");
    }

    [Fact]
    public async Task OpenSession_AlreadyOpen_ReturnsFailure()
    {
        await using var db = NewContext();
        var (_, terminalId) = await SeedTerminal(db);
        await SeedSession(db, terminalId);
        var handler = new OpenPosSessionCommandHandler(db);

        var result = await handler.Handle(
            new OpenPosSessionCommand(terminalId, Guid.NewGuid(), 500m),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already has an open session");
    }

    // === ClosePosSession ===

    [Fact]
    public async Task CloseSession_Valid_ReturnsSuccess()
    {
        await using var db = NewContext();
        var (_, terminalId) = await SeedTerminal(db);
        var sessionId = await SeedSession(db, terminalId, 500m);
        var handler = new ClosePosSessionCommandHandler(db);

        var result = await handler.Handle(
            new ClosePosSessionCommand(sessionId, 500m, "End of day"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OpeningBalance.Should().Be(500m);
        result.Value.ClosingBalance.Should().Be(500m);

        var session = await db.PosSessions.FindAsync(sessionId);
        session!.Status.Should().Be(PosSessionStatus.Closed);
    }

    [Fact]
    public async Task CloseSession_NotFound_ReturnsFailure()
    {
        await using var db = NewContext();
        var handler = new ClosePosSessionCommandHandler(db);

        var result = await handler.Handle(
            new ClosePosSessionCommand(Guid.NewGuid(), 500m),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task CloseSession_AlreadyClosed_ReturnsFailure()
    {
        await using var db = NewContext();
        var (_, terminalId) = await SeedTerminal(db);
        var sessionId = await SeedSession(db, terminalId);

        var session = await db.PosSessions.FindAsync(sessionId);
        session!.Close(500m, 500m);
        await db.SaveChangesAsync();

        var handler = new ClosePosSessionCommandHandler(db);
        var result = await handler.Handle(
            new ClosePosSessionCommand(sessionId, 500m),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not open");
    }

    // === CreatePosTransaction ===

    [Fact]
    public async Task CreateTransaction_Valid_CompletesWithFiscalDoc()
    {
        await using var db = NewContext();
        var (storeId, terminalId) = await SeedTerminal(db);
        var sessionId = await SeedSession(db, terminalId);
        var (productId, _) = await SeedProductAndStock(db, storeId);

        var queuePublisher = Substitute.For<IRsGeQueuePublisher>();
        var logger = Substitute.For<ILogger<CreatePosTransactionCommandHandler>>();
        var handler = new CreatePosTransactionCommandHandler(db, queuePublisher, logger);

        var result = await handler.Handle(new CreatePosTransactionCommand(
            sessionId,
            null,
            [new PosLineInput(productId, null, 2m, 50m)],
            [new PosPaymentInput(PaymentMethod.Cash, 100m)]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Completed");
        result.Value.Subtotal.Should().Be(100m);
        result.Value.Total.Should().Be(100m);
        result.Value.FiscalDocumentId.Should().NotBeNull();

        (await db.PosTransactions.CountAsync()).Should().Be(1);
        (await db.FiscalDocuments.CountAsync()).Should().Be(1);
        (await db.StockMovements.CountAsync()).Should().Be(1);

        await queuePublisher.Received(1).PublishAsync(
            Arg.Any<RsGeSubmissionMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTransaction_SessionNotFound_ReturnsFailure()
    {
        await using var db = NewContext();
        var queuePublisher = Substitute.For<IRsGeQueuePublisher>();
        var logger = Substitute.For<ILogger<CreatePosTransactionCommandHandler>>();
        var handler = new CreatePosTransactionCommandHandler(db, queuePublisher, logger);

        var result = await handler.Handle(new CreatePosTransactionCommand(
            Guid.NewGuid(), null,
            [new PosLineInput(Guid.NewGuid(), null, 1m, 10m)],
            [new PosPaymentInput(PaymentMethod.Cash, 10m)]),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("session not found");
    }

    [Fact]
    public async Task CreateTransaction_ClosedSession_ReturnsFailure()
    {
        await using var db = NewContext();
        var (storeId, terminalId) = await SeedTerminal(db);
        var sessionId = await SeedSession(db, terminalId);
        var session = await db.PosSessions.FindAsync(sessionId);
        session!.Close(500m, 500m);
        await db.SaveChangesAsync();

        var queuePublisher = Substitute.For<IRsGeQueuePublisher>();
        var logger = Substitute.For<ILogger<CreatePosTransactionCommandHandler>>();
        var handler = new CreatePosTransactionCommandHandler(db, queuePublisher, logger);

        var result = await handler.Handle(new CreatePosTransactionCommand(
            sessionId, null,
            [new PosLineInput(Guid.NewGuid(), null, 1m, 10m)],
            [new PosPaymentInput(PaymentMethod.Cash, 10m)]),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not open");
    }

    [Fact]
    public async Task CreateTransaction_InsufficientStock_ReturnsFailure()
    {
        await using var db = NewContext();
        var (storeId, terminalId) = await SeedTerminal(db);
        var sessionId = await SeedSession(db, terminalId);
        var (productId, _) = await SeedProductAndStock(db, storeId, stockQty: 5m);

        var queuePublisher = Substitute.For<IRsGeQueuePublisher>();
        var logger = Substitute.For<ILogger<CreatePosTransactionCommandHandler>>();
        var handler = new CreatePosTransactionCommandHandler(db, queuePublisher, logger);

        var result = await handler.Handle(new CreatePosTransactionCommand(
            sessionId, null,
            [new PosLineInput(productId, null, 10m, 50m)],
            [new PosPaymentInput(PaymentMethod.Cash, 500m)]),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Insufficient stock");
    }

    [Fact]
    public async Task CreateTransaction_InsufficientPayment_ReturnsFailure()
    {
        await using var db = NewContext();
        var (storeId, terminalId) = await SeedTerminal(db);
        var sessionId = await SeedSession(db, terminalId);
        var (productId, _) = await SeedProductAndStock(db, storeId);

        var queuePublisher = Substitute.For<IRsGeQueuePublisher>();
        var logger = Substitute.For<ILogger<CreatePosTransactionCommandHandler>>();
        var handler = new CreatePosTransactionCommandHandler(db, queuePublisher, logger);

        var result = await handler.Handle(new CreatePosTransactionCommand(
            sessionId, null,
            [new PosLineInput(productId, null, 2m, 50m)],
            [new PosPaymentInput(PaymentMethod.Cash, 50m)]),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Payment total");
    }

    // === VoidPosTransaction ===

    [Fact]
    public async Task VoidTransaction_Valid_RestoresStock()
    {
        await using var db = NewContext();
        var (storeId, terminalId) = await SeedTerminal(db);
        var sessionId = await SeedSession(db, terminalId);
        var (productId, warehouseId) = await SeedProductAndStock(db, storeId, stockQty: 100m);

        var queuePublisher = Substitute.For<IRsGeQueuePublisher>();
        var logger = Substitute.For<ILogger<CreatePosTransactionCommandHandler>>();
        var createHandler = new CreatePosTransactionCommandHandler(db, queuePublisher, logger);

        var createResult = await createHandler.Handle(new CreatePosTransactionCommand(
            sessionId, null,
            [new PosLineInput(productId, null, 5m, 20m)],
            [new PosPaymentInput(PaymentMethod.Cash, 100m)]),
            CancellationToken.None);

        createResult.IsSuccess.Should().BeTrue();

        var stockBefore = await db.StockLevels
            .FirstAsync(s => s.ProductId == productId && s.WarehouseId == warehouseId);
        var qtyBeforeVoid = stockBefore.QuantityOnHand;

        var voidHandler = new VoidPosTransactionCommandHandler(db);
        var voidResult = await voidHandler.Handle(
            new VoidPosTransactionCommand(createResult.Value!.TransactionId, Guid.NewGuid(), "Customer return"),
            CancellationToken.None);

        voidResult.IsSuccess.Should().BeTrue();

        var stockAfter = await db.StockLevels
            .FirstAsync(s => s.ProductId == productId && s.WarehouseId == warehouseId);
        stockAfter.QuantityOnHand.Should().Be(qtyBeforeVoid + 5m);

        var tx = await db.PosTransactions.FindAsync(createResult.Value.TransactionId);
        tx!.Status.Should().Be(PosTransactionStatus.Voided);
    }

    [Fact]
    public async Task VoidTransaction_NotFound_ReturnsFailure()
    {
        await using var db = NewContext();
        var handler = new VoidPosTransactionCommandHandler(db);

        var result = await handler.Handle(
            new VoidPosTransactionCommand(Guid.NewGuid(), Guid.NewGuid(), "test"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task VoidTransaction_AlreadyVoided_ReturnsFailure()
    {
        await using var db = NewContext();
        var (storeId, terminalId) = await SeedTerminal(db);
        var sessionId = await SeedSession(db, terminalId);
        var (productId, _) = await SeedProductAndStock(db, storeId);

        var queuePublisher = Substitute.For<IRsGeQueuePublisher>();
        var logger = Substitute.For<ILogger<CreatePosTransactionCommandHandler>>();
        var createHandler = new CreatePosTransactionCommandHandler(db, queuePublisher, logger);

        var createResult = await createHandler.Handle(new CreatePosTransactionCommand(
            sessionId, null,
            [new PosLineInput(productId, null, 1m, 10m)],
            [new PosPaymentInput(PaymentMethod.Cash, 10m)]),
            CancellationToken.None);

        var voidHandler = new VoidPosTransactionCommandHandler(db);
        await voidHandler.Handle(
            new VoidPosTransactionCommand(createResult.Value!.TransactionId, Guid.NewGuid(), "first void"),
            CancellationToken.None);

        var secondVoid = await voidHandler.Handle(
            new VoidPosTransactionCommand(createResult.Value.TransactionId, Guid.NewGuid(), "second void"),
            CancellationToken.None);

        secondVoid.IsFailure.Should().BeTrue();
        secondVoid.Error.Should().Contain("already voided");
    }

    // === GetPosSessions ===

    [Fact]
    public async Task GetSessions_ReturnsPagedResults()
    {
        await using var db = NewContext();
        var (_, terminalId) = await SeedTerminal(db);
        await SeedSession(db, terminalId);

        var openSession = await db.PosSessions.FirstAsync();
        openSession.Close(500m, 500m);
        await db.SaveChangesAsync();

        var terminal2 = PosTerminal.Create("TERM-002", (await db.Stores.FirstAsync()).Id, "Register 2", TerminalType.Register);
        db.PosTerminals.Add(terminal2);
        await db.SaveChangesAsync();

        await SeedSession(db, terminal2.Id);

        var handler = new GetPosSessionsQueryHandler(db);
        var result = await handler.Handle(
            new GetPosSessionsQuery(Page: 1, PageSize: 10),
            CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSessions_FilterByStatus()
    {
        await using var db = NewContext();
        var (_, terminalId) = await SeedTerminal(db);
        await SeedSession(db, terminalId);

        var handler = new GetPosSessionsQueryHandler(db);
        var result = await handler.Handle(
            new GetPosSessionsQuery(Status: "Open"),
            CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items[0].Status.Should().Be("Open");
    }

    // === GetPosTransactions ===

    [Fact]
    public async Task GetTransactions_FilterBySession()
    {
        await using var db = NewContext();
        var (storeId, terminalId) = await SeedTerminal(db);
        var sessionId = await SeedSession(db, terminalId);
        var (productId, _) = await SeedProductAndStock(db, storeId);

        var queuePublisher = Substitute.For<IRsGeQueuePublisher>();
        var logger = Substitute.For<ILogger<CreatePosTransactionCommandHandler>>();
        var createHandler = new CreatePosTransactionCommandHandler(db, queuePublisher, logger);

        await createHandler.Handle(new CreatePosTransactionCommand(
            sessionId, null,
            [new PosLineInput(productId, null, 1m, 25m)],
            [new PosPaymentInput(PaymentMethod.Cash, 25m)]),
            CancellationToken.None);

        var queryHandler = new GetPosTransactionsQueryHandler(db);
        var result = await queryHandler.Handle(
            new GetPosTransactionsQuery(SessionId: sessionId),
            CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items[0].Status.Should().Be("Completed");
    }

    [Fact]
    public async Task GetTransactions_EmptySession_ReturnsEmpty()
    {
        await using var db = NewContext();
        var handler = new GetPosTransactionsQueryHandler(db);

        var result = await handler.Handle(
            new GetPosTransactionsQuery(SessionId: Guid.NewGuid()),
            CancellationToken.None);

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }
}
