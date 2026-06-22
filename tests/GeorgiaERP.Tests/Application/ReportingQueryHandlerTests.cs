using FluentAssertions;
using GeorgiaERP.Application.Compliance;
using GeorgiaERP.Application.POS.Commands;
using GeorgiaERP.Application.Reporting.Queries;
using GeorgiaERP.Domain.Compliance;
using GeorgiaERP.Domain.Inventory;
using GeorgiaERP.Domain.Organization;
using GeorgiaERP.Domain.POS;
using GeorgiaERP.Domain.Procurement;
using GeorgiaERP.Domain.Products;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using WarehouseEntity = GeorgiaERP.Domain.Organization.Warehouse;

namespace GeorgiaERP.Tests.Application;

public class ReportingQueryHandlerTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"reporting-{Guid.NewGuid()}")
            .Options);

    private static async Task<(Guid StoreId, Guid TerminalId, Guid SessionId, Guid ProductId, Guid WarehouseId)>
        SeedFullPosSetup(AppDbContext db)
    {
        var store = Store.Create("STR-RPT", "Report Store", StoreType.Retail);
        db.Stores.Add(store);

        var terminal = PosTerminal.Create("TERM-RPT", store.Id, "Report Terminal", TerminalType.Register);
        db.PosTerminals.Add(terminal);

        var cat = Category.Create("CAT-RPT", "Report Category");
        db.Categories.Add(cat);

        var product = Product.Create("SKU-RPT", "Report Product", cat.Id, "Piece");
        db.Products.Add(product);

        var warehouse = WarehouseEntity.Create("WH-RPT", "Report Warehouse", WarehouseType.Central);
        warehouse.LinkToStore(store.Id);
        db.Warehouses.Add(warehouse);

        var stock = StockLevel.Create(product.Id, warehouse.Id, 15m);
        stock.AddStock(1000m);
        db.StockLevels.Add(stock);

        await db.SaveChangesAsync();

        var session = PosSession.Create(terminal.Id, Guid.NewGuid(), 500m);
        db.PosSessions.Add(session);
        await db.SaveChangesAsync();

        return (store.Id, terminal.Id, session.Id, product.Id, warehouse.Id);
    }

    private static async Task CreateCompletedTransaction(
        AppDbContext db, Guid sessionId, Guid productId, decimal qty, decimal unitPrice)
    {
        var queuePublisher = Substitute.For<IRsGeQueuePublisher>();
        var logger = Substitute.For<ILogger<CreatePosTransactionCommandHandler>>();
        var handler = new CreatePosTransactionCommandHandler(db, queuePublisher, logger);

        await handler.Handle(new CreatePosTransactionCommand(
            sessionId, null,
            [new PosLineInput(productId, null, qty, unitPrice)],
            [new PosPaymentInput(PaymentMethod.Cash, qty * unitPrice)]),
            CancellationToken.None);
    }

    // === DashboardKpi ===

    [Fact]
    public async Task DashboardKpi_EmptyDb_ReturnsZeros()
    {
        await using var db = NewContext();
        var handler = new DashboardKpiQueryHandler(db);

        var result = await handler.Handle(new DashboardKpiQuery(), CancellationToken.None);

        result.TotalSalesToday.Should().Be(0);
        result.TransactionsToday.Should().Be(0);
        result.TotalProducts.Should().Be(0);
        result.ActivePosTerminals.Should().Be(0);
    }

    [Fact]
    public async Task DashboardKpi_WithData_AggregatesCorrectly()
    {
        await using var db = NewContext();
        var (_, _, sessionId, productId, _) = await SeedFullPosSetup(db);

        await CreateCompletedTransaction(db, sessionId, productId, 3m, 100m);

        var handler = new DashboardKpiQueryHandler(db);
        var result = await handler.Handle(new DashboardKpiQuery(), CancellationToken.None);

        result.TotalSalesToday.Should().Be(300m);
        result.TransactionsToday.Should().Be(1);
        result.AverageTransactionValue.Should().Be(300m);
        result.TotalProducts.Should().Be(1);
        result.ActivePosTerminals.Should().Be(1);
    }

    // === SalesReport ===

    [Fact]
    public async Task SalesReport_EmptyRange_ReturnsZeros()
    {
        await using var db = NewContext();
        var handler = new SalesReportQueryHandler(db);

        var result = await handler.Handle(new SalesReportQuery(
            null,
            DateTimeOffset.UtcNow.AddDays(-7),
            DateTimeOffset.UtcNow.AddDays(1)),
            CancellationToken.None);

        result.TotalSales.Should().Be(0);
        result.TransactionCount.Should().Be(0);
    }

    [Fact]
    public async Task SalesReport_WithTransactions_CalculatesCorrectly()
    {
        await using var db = NewContext();
        var (storeId, _, sessionId, productId, _) = await SeedFullPosSetup(db);

        await CreateCompletedTransaction(db, sessionId, productId, 5m, 50m);
        await CreateCompletedTransaction(db, sessionId, productId, 2m, 75m);

        var handler = new SalesReportQueryHandler(db);
        var result = await handler.Handle(new SalesReportQuery(
            storeId,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(1)),
            CancellationToken.None);

        result.TotalSales.Should().Be(250m + 150m);
        result.TransactionCount.Should().Be(2);
        result.ByPaymentMethod.Should().NotBeEmpty();
        result.DailyBreakdown.Should().NotBeEmpty();
    }

    // === StockReport ===

    [Fact]
    public async Task StockReport_EmptyDb_ReturnsZeros()
    {
        await using var db = NewContext();
        var handler = new StockReportQueryHandler(db);

        var result = await handler.Handle(new StockReportQuery(), CancellationToken.None);

        result.TotalStockValue.Should().Be(0);
        result.TotalProducts.Should().Be(0);
    }

    [Fact]
    public async Task StockReport_WithStock_CalculatesValue()
    {
        await using var db = NewContext();
        var (_, _, _, productId, warehouseId) = await SeedFullPosSetup(db);

        var handler = new StockReportQueryHandler(db);
        var result = await handler.Handle(new StockReportQuery(warehouseId), CancellationToken.None);

        result.TotalProducts.Should().Be(1);
        result.TotalStockValue.Should().BeGreaterThan(0);
        result.Items.Should().HaveCount(1);
        result.Items[0].ProductName.Should().Be("Report Product");
    }

    // === TopSellingProducts ===

    [Fact]
    public async Task TopSelling_NoTransactions_ReturnsEmpty()
    {
        await using var db = NewContext();
        var handler = new TopSellingProductsQueryHandler(db);

        var result = await handler.Handle(new TopSellingProductsQuery(
            DateTimeOffset.UtcNow.AddDays(-30),
            DateTimeOffset.UtcNow.AddDays(1)),
            CancellationToken.None);

        result.TotalRevenue.Should().Be(0);
        result.Products.Should().BeEmpty();
    }

    [Fact]
    public async Task TopSelling_WithSales_RanksCorrectly()
    {
        await using var db = NewContext();
        var (_, _, sessionId, productId, _) = await SeedFullPosSetup(db);

        await CreateCompletedTransaction(db, sessionId, productId, 10m, 25m);

        var handler = new TopSellingProductsQueryHandler(db);
        var result = await handler.Handle(new TopSellingProductsQuery(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(1)),
            CancellationToken.None);

        result.Products.Should().HaveCount(1);
        result.Products[0].Rank.Should().Be(1);
        result.Products[0].Revenue.Should().Be(250m);
        result.Products[0].QuantitySold.Should().Be(10);
    }

    // === ProfitMarginReport ===

    [Fact]
    public async Task ProfitMargin_NoTransactions_ReturnsZeros()
    {
        await using var db = NewContext();
        var handler = new ProfitMarginReportQueryHandler(db);

        var result = await handler.Handle(new ProfitMarginReportQuery(
            DateTimeOffset.UtcNow.AddDays(-30),
            DateTimeOffset.UtcNow.AddDays(1)),
            CancellationToken.None);

        result.TotalRevenue.Should().Be(0);
        result.TotalProfit.Should().Be(0);
        result.Products.Should().BeEmpty();
    }

    [Fact]
    public async Task ProfitMargin_WithSales_CalculatesMargin()
    {
        await using var db = NewContext();
        var (_, _, sessionId, productId, _) = await SeedFullPosSetup(db);

        await CreateCompletedTransaction(db, sessionId, productId, 4m, 40m);

        var handler = new ProfitMarginReportQueryHandler(db);
        var result = await handler.Handle(new ProfitMarginReportQuery(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(1)),
            CancellationToken.None);

        result.TotalRevenue.Should().BeGreaterThan(0);
        result.Products.Should().HaveCount(1);
        result.Categories.Should().NotBeEmpty();
        result.DailyBreakdown.Should().NotBeEmpty();
        result.OverallMarginPercent.Should().BeGreaterThan(0);
    }

    // === VatReport ===

    [Fact]
    public async Task VatReport_NoDocuments_ReturnsZeros()
    {
        await using var db = NewContext();
        var handler = new VatReportQueryHandler(db);

        var result = await handler.Handle(
            new VatReportQuery(2026, 6),
            CancellationToken.None);

        result.FiscalDocumentsTotal.Should().Be(0);
        result.OutputVat.Should().Be(0);
    }

    [Fact]
    public async Task VatReport_WithDocuments_CountsCorrectly()
    {
        await using var db = NewContext();
        var doc1 = FiscalDocument.Create(FiscalDocumentType.FiscalReceipt, "INV-001");
        doc1.MarkQueued();
        var doc2 = FiscalDocument.Create(FiscalDocumentType.FiscalReceipt, "INV-002");
        doc2.MarkQueued();
        doc2.MarkSubmitted("RS-001");
        db.FiscalDocuments.AddRange(doc1, doc2);
        await db.SaveChangesAsync();

        var handler = new VatReportQueryHandler(db);
        var now = DateTimeOffset.UtcNow;
        var result = await handler.Handle(
            new VatReportQuery(now.Year, now.Month),
            CancellationToken.None);

        result.FiscalDocumentsTotal.Should().Be(2);
        result.FiscalDocumentsSubmitted.Should().Be(1);
        result.FiscalDocumentsPending.Should().Be(1);
    }

    // === SupplierPerformance ===

    [Fact]
    public async Task SupplierPerformance_NoOrders_ReturnsEmpty()
    {
        await using var db = NewContext();
        var handler = new SupplierPerformanceQueryHandler(db);

        var result = await handler.Handle(new SupplierPerformanceQuery(
            DateTimeOffset.UtcNow.AddDays(-30),
            DateTimeOffset.UtcNow.AddDays(1)),
            CancellationToken.None);

        result.TotalSuppliers.Should().Be(0);
        result.TotalOrders.Should().Be(0);
    }

    [Fact]
    public async Task SupplierPerformance_WithOrders_CalculatesMetrics()
    {
        await using var db = NewContext();
        var supplier = Supplier.Create("SUP-001", "Test Supplier");
        db.Suppliers.Add(supplier);
        var warehouse = WarehouseEntity.Create("WH-SUP", "Supplier WH", WarehouseType.Central);
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync();

        var po = PurchaseOrder.Create("PO-001", supplier.Id, warehouse.Id, Guid.NewGuid());
        po.SetTotals(1000m, 180m, 1180m);
        po.Approve(Guid.NewGuid());
        db.PurchaseOrders.Add(po);
        await db.SaveChangesAsync();

        var handler = new SupplierPerformanceQueryHandler(db);
        var result = await handler.Handle(new SupplierPerformanceQuery(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(1)),
            CancellationToken.None);

        result.TotalSuppliers.Should().Be(1);
        result.TotalOrders.Should().Be(1);
        result.Suppliers[0].SupplierName.Should().Be("Test Supplier");
        result.Suppliers[0].TotalSpend.Should().Be(1180m);
    }
}
