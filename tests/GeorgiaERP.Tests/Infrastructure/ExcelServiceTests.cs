using ClosedXML.Excel;
using FluentAssertions;
using GeorgiaERP.Application.Common;
using GeorgiaERP.Infrastructure.Reporting;
using Xunit;

namespace GeorgiaERP.Tests.Infrastructure;

public class ExcelServiceTests
{
    private readonly ExcelService _sut = new();

    private static List<ProductExportRow> SampleProductRows() =>
    [
        new ProductExportRow("SKU-001", "Test Product", "ტესტ პროდუქტი", "Electronics",
            "pcs", true, 1.5m, 10m, 100m, true, DateTimeOffset.UtcNow),
        new ProductExportRow("SKU-002", "Another Product", null, "Food",
            "kg", false, null, null, null, true, DateTimeOffset.UtcNow)
    ];

    private static List<StockExportRow> SampleStockRows() =>
    [
        new StockExportRow("SKU-001", "Test Product", "Main Warehouse",
            50m, 5m, 45m, 10.00m, 500.00m, false),
        new StockExportRow("SKU-002", "Another Product", "Store Warehouse",
            2m, 0m, 2m, 25.00m, 50.00m, true)
    ];

    [Fact]
    public void ExportProducts_ReturnsValidXlsx()
    {
        var bytes = _sut.ExportProducts(SampleProductRows());

        bytes.Should().NotBeNullOrEmpty();
        bytes[0].Should().Be(0x50);
        bytes[1].Should().Be(0x4B);
    }

    [Fact]
    public void ExportProducts_WithEmptyList_ReturnsValidXlsx()
    {
        var bytes = _sut.ExportProducts([]);

        bytes.Should().NotBeNullOrEmpty();
        bytes[0].Should().Be(0x50);
        bytes[1].Should().Be(0x4B);
    }

    [Fact]
    public void ExportStockLevels_ReturnsValidXlsx()
    {
        var bytes = _sut.ExportStockLevels(SampleStockRows());

        bytes.Should().NotBeNullOrEmpty();
        bytes[0].Should().Be(0x50);
        bytes[1].Should().Be(0x4B);
    }

    [Fact]
    public void GenerateProductImportTemplate_ReturnsValidXlsx()
    {
        var bytes = _sut.GenerateProductImportTemplate();

        bytes.Should().NotBeNullOrEmpty();
        bytes[0].Should().Be(0x50);
        bytes[1].Should().Be(0x4B);
    }

    [Fact]
    public void ParseProductImport_WithValidData_ReturnsSuccess()
    {
        var templateBytes = _sut.GenerateProductImportTemplate();
        using var stream = new MemoryStream(templateBytes);

        var result = _sut.ParseProductImport(stream);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].Sku.Should().Be("PROD-001");
        result.Value[0].Name.Should().Be("Sample Product");
        result.Value[0].CategoryCode.Should().Be("CAT-01");
        result.Value[0].UnitOfMeasure.Should().Be("pcs");
    }

    [Fact]
    public void ParseProductImport_WithMissingRequiredFields_ReturnsErrors()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Products");
        ws.Cell(1, 1).Value = "SKU*";
        ws.Cell(1, 2).Value = "Name*";
        ws.Cell(1, 3).Value = "Name (KA)";
        ws.Cell(1, 4).Value = "Category Code*";
        ws.Cell(1, 5).Value = "Unit of Measure*";

        ws.Cell(2, 1).Value = "SKU-001";
        ws.Cell(2, 2).Value = "";
        ws.Cell(2, 3).Value = "";
        ws.Cell(2, 4).Value = "";
        ws.Cell(2, 5).Value = "";

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;

        var result = _sut.ParseProductImport(ms);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void ParseProductImport_WithEmptyFile_ReturnsEmptyList()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Products");
        ws.Cell(1, 1).Value = "SKU*";
        ws.Cell(1, 2).Value = "Name*";

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;

        var result = _sut.ParseProductImport(ms);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
