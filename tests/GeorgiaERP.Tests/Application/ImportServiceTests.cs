using System.Text;
using ClosedXML.Excel;
using GeorgiaERP.Infrastructure.Import;
using FluentAssertions;
using Xunit;

namespace GeorgiaERP.Tests.Application;

public class ImportServiceTests
{
    private readonly ImportService _sut = new();

    // ── CSV Parsing ──────────────────────────────────────────────

    [Fact]
    public void ParseRows_Csv_ParsesBasicFile()
    {
        var csv = "Name,Age,Active\nAlice,30,true\nBob,25,false";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var rows = _sut.ParseRows(stream, "text/csv");

        rows.Should().HaveCount(2);
        rows[0]["Name"].Should().Be("Alice");
        rows[0]["Age"].Should().Be("30");
        rows[0]["Active"].Should().Be("true");
        rows[1]["Name"].Should().Be("Bob");
    }

    [Fact]
    public void ParseRows_Csv_HandlesQuotedFields()
    {
        var csv = "Name,Description\n\"Smith, John\",\"A value with \"\"quotes\"\"\"";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var rows = _sut.ParseRows(stream, "text/csv");

        rows.Should().HaveCount(1);
        rows[0]["Name"].Should().Be("Smith, John");
        rows[0]["Description"].Should().Be("A value with \"quotes\"");
    }

    [Fact]
    public void ParseRows_Csv_HandlesUtf8Bom()
    {
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var csvBytes = Encoding.UTF8.GetBytes("Name,Value\nTest,123");
        var withBom = bom.Concat(csvBytes).ToArray();
        using var stream = new MemoryStream(withBom);

        var rows = _sut.ParseRows(stream, "text/csv");

        rows.Should().HaveCount(1);
        rows[0].Should().ContainKey("Name");
        rows[0]["Name"].Should().Be("Test");
    }

    [Fact]
    public void ParseRows_Csv_SkipsEmptyLines()
    {
        var csv = "Name,Value\nAlice,1\n\nBob,2\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var rows = _sut.ParseRows(stream, "text/csv");

        rows.Should().HaveCount(2);
    }

    [Fact]
    public void ParseRows_Csv_EmptyFile_ReturnsEmpty()
    {
        using var stream = new MemoryStream(Array.Empty<byte>());

        var rows = _sut.ParseRows(stream, "text/csv");

        rows.Should().BeEmpty();
    }

    [Fact]
    public void ParseRows_Csv_HeaderOnly_ReturnsEmpty()
    {
        var csv = "Name,Value";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var rows = _sut.ParseRows(stream, "text/csv");

        rows.Should().BeEmpty();
    }

    [Fact]
    public void ParseRows_Csv_CaseInsensitiveHeaders()
    {
        var csv = "NAME,value\nTest,123";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var rows = _sut.ParseRows(stream, "text/csv");

        rows[0]["name"].Should().Be("Test");
        rows[0]["NAME"].Should().Be("Test");
        rows[0]["Value"].Should().Be("123");
    }

    [Fact]
    public void ParseRows_Csv_MissingTrailingFields_DefaultToEmpty()
    {
        var csv = "A,B,C\n1";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var rows = _sut.ParseRows(stream, "text/csv");

        rows[0]["A"].Should().Be("1");
        rows[0]["B"].Should().Be(string.Empty);
        rows[0]["C"].Should().Be(string.Empty);
    }

    // ── Excel Parsing ────────────────────────────────────────────

    [Fact]
    public void ParseRows_Excel_ParsesBasicWorkbook()
    {
        var excelBytes = CreateExcelFile(
            new[] { "Name", "Age", "Active" },
            new[]
            {
                new object[] { "Alice", 30, "true" },
                new object[] { "Bob", 25, "false" },
            });

        using var stream = new MemoryStream(excelBytes);

        var rows = _sut.ParseRows(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        rows.Should().HaveCount(2);
        rows[0]["Name"].Should().Be("Alice");
        rows[0]["Age"].Should().Be("30");
        rows[1]["Name"].Should().Be("Bob");
    }

    [Fact]
    public void ParseRows_Excel_HandlesNumericValues()
    {
        var excelBytes = CreateExcelFile(
            new[] { "Price", "Quantity" },
            new[]
            {
                new object[] { 99.95, 42 },
            });

        using var stream = new MemoryStream(excelBytes);

        var rows = _sut.ParseRows(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        rows.Should().HaveCount(1);
        rows[0]["Price"].Should().Be("99.95");
        rows[0]["Quantity"].Should().Be("42");
    }

    [Fact]
    public void ParseRows_Excel_SkipsEmptyRows()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Data");
        ws.Cell(1, 1).Value = "Name";
        ws.Cell(2, 1).Value = "Alice";
        // Row 3 is empty
        ws.Cell(4, 1).Value = "Bob";

        using var outStream = new MemoryStream();
        workbook.SaveAs(outStream);
        outStream.Position = 0;

        var rows = _sut.ParseRows(outStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        // ClosedXML reports the used range up to row 4, but row 3 is empty and skipped
        rows.Should().HaveCount(2);
    }

    [Fact]
    public void ParseRows_Excel_CaseInsensitiveHeaders()
    {
        var excelBytes = CreateExcelFile(
            new[] { "SKU", "name" },
            new[]
            {
                new object[] { "P001", "Widget" },
            });

        using var stream = new MemoryStream(excelBytes);

        var rows = _sut.ParseRows(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        rows[0]["sku"].Should().Be("P001");
        rows[0]["NAME"].Should().Be("Widget");
    }

    [Fact]
    public void ParseRows_DetectsExcelByContentType()
    {
        var excelBytes = CreateExcelFile(
            new[] { "Name" },
            new[] { new object[] { "Test" } });

        using var stream = new MemoryStream(excelBytes);

        // Should detect Excel from content type containing "spreadsheet"
        var rows = _sut.ParseRows(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        rows.Should().HaveCount(1);
    }

    // ── Helpers ───────────────────────────────────────────────────

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
