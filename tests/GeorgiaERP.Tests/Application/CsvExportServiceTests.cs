using System.Text;
using GeorgiaERP.Application.Common;
using GeorgiaERP.Infrastructure.Export;
using FluentAssertions;
using Xunit;

namespace GeorgiaERP.Tests.Application;

public class CsvExportServiceTests
{
    private readonly CsvExportService _sut = new();

    [Fact]
    public void ToCsv_EmptyCollection_ReturnsHeaderOnly()
    {
        var columns = new List<ExportColumn<TestItem>>
        {
            new() { Header = "Name", Selector = x => x.Name },
            new() { Header = "Value", Selector = x => x.Value },
        };

        var result = _sut.ToCsv(Array.Empty<TestItem>(), columns);
        var csv = GetCsvContent(result);

        csv.Should().StartWith("Name,Value");
        csv.Trim().Split('\n').Should().HaveCount(1); // Header only
    }

    [Fact]
    public void ToCsv_WithItems_ReturnsHeaderAndDataRows()
    {
        var items = new[]
        {
            new TestItem("Alice", 100, true, new DateTime(2025, 1, 15)),
            new TestItem("Bob", 200, false, new DateTime(2025, 6, 30)),
        };

        var columns = new List<ExportColumn<TestItem>>
        {
            new() { Header = "Name", Selector = x => x.Name },
            new() { Header = "Value", Selector = x => x.Value, Format = "N2" },
            new() { Header = "Active", Selector = x => x.Active },
            new() { Header = "Date", Selector = x => x.Created },
        };

        var result = _sut.ToCsv(items, columns);
        var csv = GetCsvContent(result);
        var lines = csv.Trim().Split('\n').Select(l => l.Trim('\r')).ToArray();

        lines.Should().HaveCount(3);
        lines[0].Should().Be("Name,Value,Active,Date");
        lines[1].Should().Contain("Alice");
        lines[1].Should().Contain("100.00");
        lines[1].Should().Contain("Yes");
        lines[2].Should().Contain("Bob");
        lines[2].Should().Contain("No");
    }

    [Fact]
    public void ToCsv_EscapesCommasInValues()
    {
        var items = new[] { new TestItem("Smith, John", 50, true, DateTime.Now) };

        var columns = new List<ExportColumn<TestItem>>
        {
            new() { Header = "Name", Selector = x => x.Name },
        };

        var result = _sut.ToCsv(items, columns);
        var csv = GetCsvContent(result);

        csv.Should().Contain("\"Smith, John\"");
    }

    [Fact]
    public void ToCsv_EscapesDoubleQuotesInValues()
    {
        var items = new[] { new TestItem("She said \"hello\"", 0, true, DateTime.Now) };

        var columns = new List<ExportColumn<TestItem>>
        {
            new() { Header = "Name", Selector = x => x.Name },
        };

        var result = _sut.ToCsv(items, columns);
        var csv = GetCsvContent(result);

        csv.Should().Contain("\"She said \"\"hello\"\"\"");
    }

    [Fact]
    public void ToCsv_HandlesNullValues()
    {
        var items = new[] { new TestItem(null!, 0, true, DateTime.Now) };

        var columns = new List<ExportColumn<TestItem>>
        {
            new() { Header = "Name", Selector = x => x.Name },
            new() { Header = "Value", Selector = x => (object?)null },
        };

        var result = _sut.ToCsv(items, columns);
        var csv = GetCsvContent(result);
        var dataLine = csv.Trim().Split('\n')[1].Trim('\r');

        // Null values should be empty strings
        dataLine.Should().Be(",");
    }

    [Fact]
    public void ToCsv_IncludesUtf8Bom()
    {
        var columns = new List<ExportColumn<TestItem>>
        {
            new() { Header = "Name", Selector = x => x.Name },
        };

        var result = _sut.ToCsv(Array.Empty<TestItem>(), columns);

        // UTF-8 BOM: EF BB BF
        result[0].Should().Be(0xEF);
        result[1].Should().Be(0xBB);
        result[2].Should().Be(0xBF);
    }

    [Fact]
    public void ToCsv_EmptyColumns_ReturnsOnlyBom()
    {
        var items = new[] { new TestItem("test", 1, true, DateTime.Now) };
        var result = _sut.ToCsv(items, new List<ExportColumn<TestItem>>());

        result.Should().HaveCount(3); // Just the BOM
    }

    [Fact]
    public void ToCsv_DecimalFormat_UsesInvariantCulture()
    {
        var items = new[] { new TestItem("test", 99.95m, true, DateTime.Now) };

        var columns = new List<ExportColumn<TestItem>>
        {
            new() { Header = "Name", Selector = x => x.Name },
            new() { Header = "Value", Selector = x => x.Value, Format = "N2" },
        };

        var result = _sut.ToCsv(items, columns);
        var csv = GetCsvContent(result);

        // InvariantCulture N2 format with no thousands separator needed: "99.95"
        csv.Should().Contain("99.95");
    }

    [Fact]
    public void ToCsv_BoolValues_DisplayAsYesNo()
    {
        var items = new[]
        {
            new TestItem("active", 0, true, DateTime.Now),
            new TestItem("inactive", 0, false, DateTime.Now),
        };

        var columns = new List<ExportColumn<TestItem>>
        {
            new() { Header = "Active", Selector = x => x.Active },
        };

        var result = _sut.ToCsv(items, columns);
        var csv = GetCsvContent(result);
        var lines = csv.Trim().Split('\n').Select(l => l.Trim('\r')).ToArray();

        lines[1].Should().Be("Yes");
        lines[2].Should().Be("No");
    }

    [Fact]
    public void ToCsv_EscapesNewlinesInValues()
    {
        var items = new[] { new TestItem("Line1\nLine2", 0, true, DateTime.Now) };

        var columns = new List<ExportColumn<TestItem>>
        {
            new() { Header = "Name", Selector = x => x.Name },
        };

        var result = _sut.ToCsv(items, columns);
        var csv = GetCsvContent(result);

        // Value containing newline should be quoted
        csv.Should().Contain("\"Line1\nLine2\"");
    }

    private static string GetCsvContent(byte[] bytes)
    {
        // Skip UTF-8 BOM (3 bytes)
        return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
    }

    private record TestItem(string Name, decimal Value, bool Active, DateTime Created);
}
