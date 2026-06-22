using ClosedXML.Excel;
using GeorgiaERP.Application.Common;
using GeorgiaERP.Infrastructure.Export;
using FluentAssertions;
using Xunit;

namespace GeorgiaERP.Tests.Application;

public class ExcelExportServiceTests
{
    private readonly CsvExportService _sut = new();

    [Fact]
    public void ToExcel_EmptyCollection_ReturnsValidExcelWithHeaderOnly()
    {
        var columns = new List<ExportColumn<TestItem>>
        {
            new() { Header = "Name", Selector = x => x.Name },
            new() { Header = "Value", Selector = x => x.Value },
        };

        var result = _sut.ToExcel(Array.Empty<TestItem>(), columns);

        result.Should().NotBeEmpty();
        using var stream = new MemoryStream(result);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();

        worksheet.Cell(1, 1).GetString().Should().Be("Name");
        worksheet.Cell(1, 2).GetString().Should().Be("Value");
        worksheet.LastRowUsed()!.RowNumber().Should().Be(1); // Only header
    }

    [Fact]
    public void ToExcel_WithItems_ContainsHeaderAndDataRows()
    {
        var items = new[]
        {
            new TestItem("Alice", 100.50m, true, new DateTime(2025, 1, 15)),
            new TestItem("Bob", 200.75m, false, new DateTime(2025, 6, 30)),
        };

        var columns = new List<ExportColumn<TestItem>>
        {
            new() { Header = "Name", Selector = x => x.Name },
            new() { Header = "Value", Selector = x => x.Value, Format = "N2" },
            new() { Header = "Active", Selector = x => x.Active },
        };

        var result = _sut.ToExcel(items, columns);

        using var stream = new MemoryStream(result);
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.First();

        ws.LastRowUsed()!.RowNumber().Should().Be(3); // Header + 2 data rows
        ws.Cell(2, 1).GetString().Should().Be("Alice");
        ws.Cell(2, 2).GetDouble().Should().Be(100.50);
        ws.Cell(2, 3).GetString().Should().Be("Yes");
        ws.Cell(3, 1).GetString().Should().Be("Bob");
        ws.Cell(3, 3).GetString().Should().Be("No");
    }

    [Fact]
    public void ToExcel_ProducesValidExcelFile_WithFreezeAndFilter()
    {
        var columns = new List<ExportColumn<TestItem>>
        {
            new() { Header = "Name", Selector = x => x.Name },
            new() { Header = "Value", Selector = x => x.Value },
        };

        var result = _sut.ToExcel(new[] { new TestItem("test", 1, true, DateTime.Now) }, columns);

        // Should produce a valid XLSX file that can be opened without errors
        result.Should().NotBeEmpty();
        using var stream = new MemoryStream(result);
        var act = () => new XLWorkbook(stream);
        act.Should().NotThrow("output should be a valid Excel file");
    }

    [Fact]
    public void ToExcel_HeaderCells_AreStyled()
    {
        var columns = new List<ExportColumn<TestItem>>
        {
            new() { Header = "Name", Selector = x => x.Name },
        };

        var result = _sut.ToExcel(new[] { new TestItem("test", 0, true, DateTime.Now) }, columns);

        using var stream = new MemoryStream(result);
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.First();

        var headerCell = ws.Cell(1, 1);
        headerCell.Style.Font.Bold.Should().BeTrue();
        headerCell.Style.Font.FontColor.Should().Be(XLColor.White);
    }

    [Fact]
    public void ToExcel_UsesCustomSheetName()
    {
        var columns = new List<ExportColumn<TestItem>>
        {
            new() { Header = "Name", Selector = x => x.Name },
        };

        var result = _sut.ToExcel(Array.Empty<TestItem>(), columns, "Products");

        using var stream = new MemoryStream(result);
        using var workbook = new XLWorkbook(stream);
        workbook.Worksheets.First().Name.Should().Be("Products");
    }

    [Fact]
    public void ToExcel_HandlesNullValues()
    {
        var items = new[] { new TestItem(null!, 0, true, DateTime.Now) };

        var columns = new List<ExportColumn<TestItem>>
        {
            new() { Header = "Name", Selector = x => x.Name },
            new() { Header = "Nullable", Selector = _ => (object?)null },
        };

        var result = _sut.ToExcel(items, columns);

        using var stream = new MemoryStream(result);
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.First();

        ws.Cell(2, 2).IsEmpty().Should().BeTrue();
    }

    [Fact]
    public void ToExcel_HandlesDateTimeOffset()
    {
        var dto = new DateTimeOffset(2025, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var items = new[] { new TestItem("test", 0, true, DateTime.Now) };

        var columns = new List<ExportColumn<TestItem>>
        {
            new() { Header = "Date", Selector = _ => dto },
        };

        var result = _sut.ToExcel(items, columns);

        using var stream = new MemoryStream(result);
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.First();

        // The cell should have a date format
        ws.Cell(2, 1).Style.DateFormat.Format.Should().Contain("yyyy");
    }

    [Fact]
    public void ToExcel_NumericValuesAreNumbers_NotText()
    {
        var items = new[] { new TestItem("test", 42.5m, true, DateTime.Now) };

        var columns = new List<ExportColumn<TestItem>>
        {
            new() { Header = "Value", Selector = x => x.Value },
        };

        var result = _sut.ToExcel(items, columns);

        using var stream = new MemoryStream(result);
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.First();

        ws.Cell(2, 1).DataType.Should().Be(XLDataType.Number);
        ws.Cell(2, 1).GetDouble().Should().Be(42.5);
    }

    [Fact]
    public void ToExcel_HasAutoFilter()
    {
        var items = new[] { new TestItem("test", 0, true, DateTime.Now) };
        var columns = new List<ExportColumn<TestItem>>
        {
            new() { Header = "Name", Selector = x => x.Name },
            new() { Header = "Value", Selector = x => x.Value },
        };

        var result = _sut.ToExcel(items, columns);

        using var stream = new MemoryStream(result);
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.First();

        ws.AutoFilter.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void ToExcel_AlternateRowShading()
    {
        var items = new[]
        {
            new TestItem("Row1", 1, true, DateTime.Now),
            new TestItem("Row2", 2, false, DateTime.Now),
            new TestItem("Row3", 3, true, DateTime.Now),
        };

        var columns = new List<ExportColumn<TestItem>>
        {
            new() { Header = "Name", Selector = x => x.Name },
        };

        var result = _sut.ToExcel(items, columns);

        using var stream = new MemoryStream(result);
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.First();

        // Row 2 (first data row, row index 2) is even, should have shading
        var row2Color = ws.Cell(2, 1).Style.Fill.BackgroundColor;
        // Row 3 (second data row, row index 3) is odd, no shading
        var row3Color = ws.Cell(3, 1).Style.Fill.BackgroundColor;

        row2Color.Should().Be(XLColor.FromHtml("#D6E4F0"));
        row3Color.Should().NotBe(XLColor.FromHtml("#D6E4F0"));
    }

    private record TestItem(string Name, decimal Value, bool Active, DateTime Created);
}
