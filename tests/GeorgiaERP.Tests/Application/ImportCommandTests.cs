using System.Text;
using ClosedXML.Excel;
using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Import;
using GeorgiaERP.Domain.Organization;
using GeorgiaERP.Domain.Products;
using GeorgiaERP.Infrastructure.Import;
using GeorgiaERP.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Warehouse = GeorgiaERP.Domain.Organization.Warehouse;

namespace GeorgiaERP.Tests.Application;

public class ImportCommandTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ImportService _importService;

    public ImportCommandTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _importService = new ImportService();

        // Seed a category for product import tests
        var category = Category.Create("Electronics", "Electronics", null);
        _db.Categories.Add(category);
        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    // ── Product Import ──────────────────────────────────────────

    [Fact]
    public async Task ImportProducts_ValidCsv_CreatesProducts()
    {
        var categoryId = _db.Categories.First().Id;
        var csv = $"SKU,Name,CategoryId,UnitOfMeasure,VatApplicable\nIMPORT-001,Test Widget,{categoryId},pcs,true\nIMPORT-002,Test Gadget,{categoryId},kg,false";

        var handler = new ImportProductsHandler(_db, _importService);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = await handler.Handle(
            new ImportProductsCommand(stream, "text/csv"),
            CancellationToken.None);

        result.TotalRows.Should().Be(2);
        result.SuccessCount.Should().Be(2);
        result.ErrorCount.Should().Be(0);
        result.Errors.Should().BeEmpty();

        _db.Products.Count().Should().Be(2);
        _db.Products.Any(p => p.Sku == "IMPORT-001").Should().BeTrue();
        _db.Products.Any(p => p.Sku == "IMPORT-002").Should().BeTrue();
    }

    [Fact]
    public async Task ImportProducts_MissingRequiredFields_ReturnsErrors()
    {
        var csv = "SKU,Name,CategoryId,UnitOfMeasure\n,,invalid-guid,";

        var handler = new ImportProductsHandler(_db, _importService);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = await handler.Handle(
            new ImportProductsCommand(stream, "text/csv"),
            CancellationToken.None);

        result.TotalRows.Should().Be(1);
        result.SuccessCount.Should().Be(0);
        result.ErrorCount.Should().Be(1);
        result.Errors.Should().HaveCountGreaterThan(0);

        // Should have errors for SKU, Name, CategoryId, UnitOfMeasure
        result.Errors.Select(e => e.Field).Should().Contain("SKU");
        result.Errors.Select(e => e.Field).Should().Contain("Name");
        result.Errors.Select(e => e.Field).Should().Contain("CategoryId");
        result.Errors.Select(e => e.Field).Should().Contain("UnitOfMeasure");
    }

    [Fact]
    public async Task ImportProducts_DuplicateSku_ReturnsError()
    {
        var categoryId = _db.Categories.First().Id;

        // Create an existing product
        var existing = Product.Create("EXISTING-001", "Existing", categoryId, "pcs");
        _db.Products.Add(existing);
        await _db.SaveChangesAsync();

        var csv = $"SKU,Name,CategoryId,UnitOfMeasure\nEXISTING-001,Duplicate,{categoryId},pcs";

        var handler = new ImportProductsHandler(_db, _importService);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = await handler.Handle(
            new ImportProductsCommand(stream, "text/csv"),
            CancellationToken.None);

        result.SuccessCount.Should().Be(0);
        result.Errors.Should().Contain(e => e.Field == "SKU" && e.Error == "SKU already exists");
    }

    [Fact]
    public async Task ImportProducts_InvalidCategoryId_ReturnsError()
    {
        var fakeCategoryId = Guid.NewGuid();
        var csv = $"SKU,Name,CategoryId,UnitOfMeasure\nNEW-001,Test,{fakeCategoryId},pcs";

        var handler = new ImportProductsHandler(_db, _importService);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = await handler.Handle(
            new ImportProductsCommand(stream, "text/csv"),
            CancellationToken.None);

        result.SuccessCount.Should().Be(0);
        result.Errors.Should().Contain(e => e.Field == "CategoryId" && e.Error == "Category not found");
    }

    [Fact]
    public async Task ImportProducts_MixedValidAndInvalid_PartialSuccess()
    {
        var categoryId = _db.Categories.First().Id;
        var csv = $"SKU,Name,CategoryId,UnitOfMeasure\nVALID-001,Good Product,{categoryId},pcs\n,,invalid,";

        var handler = new ImportProductsHandler(_db, _importService);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = await handler.Handle(
            new ImportProductsCommand(stream, "text/csv"),
            CancellationToken.None);

        result.TotalRows.Should().Be(2);
        result.SuccessCount.Should().Be(1);
        result.ErrorCount.Should().Be(1);
    }

    [Fact]
    public async Task ImportProducts_EmptyFile_ReturnsZeroCounts()
    {
        var csv = "";

        var handler = new ImportProductsHandler(_db, _importService);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = await handler.Handle(
            new ImportProductsCommand(stream, "text/csv"),
            CancellationToken.None);

        result.TotalRows.Should().Be(0);
        result.SuccessCount.Should().Be(0);
        result.ErrorCount.Should().Be(0);
    }

    [Fact]
    public async Task ImportProducts_InvalidDecimalField_ReturnsError()
    {
        var categoryId = _db.Categories.First().Id;
        var csv = $"SKU,Name,CategoryId,UnitOfMeasure,WeightKg\nDEC-001,Test,{categoryId},pcs,not-a-number";

        var handler = new ImportProductsHandler(_db, _importService);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = await handler.Handle(
            new ImportProductsCommand(stream, "text/csv"),
            CancellationToken.None);

        result.Errors.Should().Contain(e => e.Field == "WeightKg" && e.Error == "Invalid number format");
    }

    [Fact]
    public async Task ImportProducts_OptionalFields_AreSet()
    {
        var categoryId = _db.Categories.First().Id;
        var csv = $"SKU,Name,CategoryId,UnitOfMeasure,NameKa,Description,VatApplicable,WeightKg,MinStockLevel\n" +
                  $"OPT-001,Optional Test,{categoryId},pcs,ტესტი,A description,yes,1.5,10";

        var handler = new ImportProductsHandler(_db, _importService);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = await handler.Handle(
            new ImportProductsCommand(stream, "text/csv"),
            CancellationToken.None);

        result.SuccessCount.Should().Be(1);

        var product = _db.Products.First(p => p.Sku == "OPT-001");
        product.NameKa.Should().Be("ტესტი");
        product.Description.Should().Be("A description");
        product.VatApplicable.Should().BeTrue();
        product.WeightKg.Should().Be(1.5m);
        product.MinStockLevel.Should().Be(10m);
    }

    [Fact]
    public async Task ImportProducts_IntraBatchDuplicates_DetectsSecondOccurrence()
    {
        var categoryId = _db.Categories.First().Id;
        var csv = $"SKU,Name,CategoryId,UnitOfMeasure\nDUP-001,First,{categoryId},pcs\nDUP-001,Duplicate,{categoryId},pcs";

        var handler = new ImportProductsHandler(_db, _importService);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = await handler.Handle(
            new ImportProductsCommand(stream, "text/csv"),
            CancellationToken.None);

        result.SuccessCount.Should().Be(1);
        result.ErrorCount.Should().Be(1);
        result.Errors.Should().Contain(e => e.Field == "SKU" && e.Error == "SKU already exists");
    }

    // ── Inventory Import ────────────────────────────────────────

    [Fact]
    public async Task ImportInventory_ValidData_CreatesStockLevels()
    {
        var categoryId = _db.Categories.First().Id;
        var product = Product.Create("INV-001", "Inventory Test", categoryId, "pcs");
        _db.Products.Add(product);

        var warehouse = Warehouse.Create("WH-A", "Warehouse A", WarehouseType.Central);
        _db.Warehouses.Add(warehouse);
        await _db.SaveChangesAsync();

        var csv = $"SKU,WarehouseId,Quantity,CostPrice\nINV-001,{warehouse.Id},50,10.99";

        var handler = new ImportInventoryHandler(_db, _importService);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = await handler.Handle(
            new ImportInventoryCommand(stream, "text/csv"),
            CancellationToken.None);

        result.TotalRows.Should().Be(1);
        result.SuccessCount.Should().Be(1);
        result.ErrorCount.Should().Be(0);

        var stockLevel = _db.StockLevels.First();
        stockLevel.ProductId.Should().Be(product.Id);
        stockLevel.WarehouseId.Should().Be(warehouse.Id);
        stockLevel.QuantityOnHand.Should().Be(50);
    }

    [Fact]
    public async Task ImportInventory_UnknownSku_ReturnsError()
    {
        var warehouse = Warehouse.Create("WH-B", "Warehouse B", WarehouseType.Central);
        _db.Warehouses.Add(warehouse);
        await _db.SaveChangesAsync();

        var csv = $"SKU,WarehouseId,Quantity\nNONEXISTENT,{warehouse.Id},10";

        var handler = new ImportInventoryHandler(_db, _importService);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = await handler.Handle(
            new ImportInventoryCommand(stream, "text/csv"),
            CancellationToken.None);

        result.SuccessCount.Should().Be(0);
        result.Errors.Should().Contain(e => e.Field == "SKU" && e.Error == "Product not found");
    }

    [Fact]
    public async Task ImportInventory_InvalidQuantity_ReturnsError()
    {
        var categoryId = _db.Categories.First().Id;
        var product = Product.Create("QTY-001", "Test", categoryId, "pcs");
        _db.Products.Add(product);

        var warehouse = Warehouse.Create("WH-C", "Warehouse C", WarehouseType.Central);
        _db.Warehouses.Add(warehouse);
        await _db.SaveChangesAsync();

        var csv = $"SKU,WarehouseId,Quantity\nQTY-001,{warehouse.Id},not-a-number";

        var handler = new ImportInventoryHandler(_db, _importService);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = await handler.Handle(
            new ImportInventoryCommand(stream, "text/csv"),
            CancellationToken.None);

        result.Errors.Should().Contain(e => e.Field == "Quantity" && e.Error == "Invalid number format");
    }

    [Fact]
    public async Task ImportInventory_NegativeQuantity_ReturnsError()
    {
        var categoryId = _db.Categories.First().Id;
        var product = Product.Create("NEG-001", "Test", categoryId, "pcs");
        _db.Products.Add(product);

        var warehouse = Warehouse.Create("WH-D", "Warehouse D", WarehouseType.Central);
        _db.Warehouses.Add(warehouse);
        await _db.SaveChangesAsync();

        var csv = $"SKU,WarehouseId,Quantity\nNEG-001,{warehouse.Id},-5";

        var handler = new ImportInventoryHandler(_db, _importService);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = await handler.Handle(
            new ImportInventoryCommand(stream, "text/csv"),
            CancellationToken.None);

        result.Errors.Should().Contain(e => e.Field == "Quantity" && e.Error == "Quantity must be positive");
    }

    [Fact]
    public async Task ImportInventory_ExcelFile_Works()
    {
        var categoryId = _db.Categories.First().Id;
        var product = Product.Create("XLSX-001", "Excel Test", categoryId, "pcs");
        _db.Products.Add(product);

        var warehouse = Warehouse.Create("WH-E", "Warehouse E", WarehouseType.Central);
        _db.Warehouses.Add(warehouse);
        await _db.SaveChangesAsync();

        var excelBytes = CreateExcelFile(
            new[] { "SKU", "WarehouseId", "Quantity" },
            new[] { new object[] { "XLSX-001", warehouse.Id.ToString(), 100 } });

        var handler = new ImportInventoryHandler(_db, _importService);
        using var stream = new MemoryStream(excelBytes);
        var result = await handler.Handle(
            new ImportInventoryCommand(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
            CancellationToken.None);

        result.SuccessCount.Should().Be(1);
        _db.StockLevels.First(s => s.ProductId == product.Id).QuantityOnHand.Should().Be(100);
    }

    private static byte[] CreateExcelFile(string[] headers, object[][] dataRows)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Data");

        for (var col = 0; col < headers.Length; col++)
            ws.Cell(1, col + 1).Value = headers[col];

        for (var row = 0; row < dataRows.Length; row++)
        {
            for (var col = 0; col < dataRows[row].Length; col++)
            {
                var value = dataRows[row][col];
                var cell = ws.Cell(row + 2, col + 1);
                switch (value)
                {
                    case int i: cell.Value = i; break;
                    case double d: cell.Value = d; break;
                    case decimal dec: cell.Value = (double)dec; break;
                    default: cell.Value = value?.ToString() ?? string.Empty; break;
                }
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
