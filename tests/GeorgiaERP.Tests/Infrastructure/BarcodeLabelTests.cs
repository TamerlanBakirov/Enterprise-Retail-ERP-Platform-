using System.Text;
using FluentAssertions;
using GeorgiaERP.Application.Common;
using GeorgiaERP.Infrastructure.Reporting;
using Xunit;

namespace GeorgiaERP.Tests.Infrastructure;

public class BarcodeLabelTests
{
    private readonly PdfGenerationService _sut = new();

    private static List<BarcodeLabelData> SampleLabels(int count = 3, bool includePrice = true) =>
        Enumerable.Range(1, count).Select(i => new BarcodeLabelData(
            Barcode: $"490123456789{i % 10}",
            BarcodeType: "Ean13",
            ProductName: $"Test Product {i}",
            Sku: $"SKU-{i:D4}",
            Price: includePrice ? 9.99m + i : null,
            Currency: includePrice ? "GEL" : null)).ToList();

    [Fact]
    public void GenerateBarcodeLabels_WithValidData_ReturnsValidPdf()
    {
        var labels = SampleLabels();

        var pdf = _sut.GenerateBarcodeLabels(labels, BarcodeLabelSize.Medium);

        pdf.Should().NotBeNullOrEmpty();
        pdf.Length.Should().BeGreaterThan(100);
        Encoding.ASCII.GetString(pdf, 0, 5).Should().Be("%PDF-");
    }

    [Fact]
    public void GenerateBarcodeLabels_WithSingleLabel_ReturnsValidPdf()
    {
        var labels = SampleLabels(count: 1);

        var pdf = _sut.GenerateBarcodeLabels(labels, BarcodeLabelSize.Medium);

        pdf.Should().NotBeNullOrEmpty();
        Encoding.ASCII.GetString(pdf, 0, 5).Should().Be("%PDF-");
    }

    [Theory]
    [InlineData(BarcodeLabelSize.Small)]
    [InlineData(BarcodeLabelSize.Medium)]
    [InlineData(BarcodeLabelSize.Large)]
    public void GenerateBarcodeLabels_AllSizes_ProduceValidPdf(BarcodeLabelSize size)
    {
        var labels = SampleLabels();

        var pdf = _sut.GenerateBarcodeLabels(labels, size);

        pdf.Should().NotBeNullOrEmpty();
        Encoding.ASCII.GetString(pdf, 0, 5).Should().Be("%PDF-");
    }

    [Fact]
    public void GenerateBarcodeLabels_WithoutPrices_ReturnsValidPdf()
    {
        var labels = SampleLabels(includePrice: false);

        var pdf = _sut.GenerateBarcodeLabels(labels, BarcodeLabelSize.Medium);

        pdf.Should().NotBeNullOrEmpty();
        Encoding.ASCII.GetString(pdf, 0, 5).Should().Be("%PDF-");
    }

    [Fact]
    public void GenerateBarcodeLabels_WithPrices_ProducesLargerPdf()
    {
        var withPrices = SampleLabels(includePrice: true);
        var withoutPrices = SampleLabels(includePrice: false);

        var pdfWith = _sut.GenerateBarcodeLabels(withPrices, BarcodeLabelSize.Medium);
        var pdfWithout = _sut.GenerateBarcodeLabels(withoutPrices, BarcodeLabelSize.Medium);

        pdfWith.Length.Should().BeGreaterThan(pdfWithout.Length);
    }

    [Fact]
    public void GenerateBarcodeLabels_ManyLabels_SpansMultiplePages()
    {
        var labels = SampleLabels(count: 25);

        var pdf = _sut.GenerateBarcodeLabels(labels, BarcodeLabelSize.Medium);

        pdf.Should().NotBeNullOrEmpty();
        Encoding.ASCII.GetString(pdf, 0, 5).Should().Be("%PDF-");
        pdf.Length.Should().BeGreaterThan(500);
    }

    [Fact]
    public void GenerateBarcodeLabels_WithLongProductName_TruncatesAndReturnsValidPdf()
    {
        var labels = new List<BarcodeLabelData>
        {
            new(
                Barcode: "4901234567890",
                BarcodeType: "Ean13",
                ProductName: "This Is A Very Long Product Name That Should Be Truncated In The Label Output",
                Sku: "SKU-LONG",
                Price: 99.99m,
                Currency: "GEL")
        };

        var pdf = _sut.GenerateBarcodeLabels(labels, BarcodeLabelSize.Small);

        pdf.Should().NotBeNullOrEmpty();
        Encoding.ASCII.GetString(pdf, 0, 5).Should().Be("%PDF-");
    }

    [Fact]
    public void GenerateBarcodeLabels_WithNullSkuAndPrice_ReturnsValidPdf()
    {
        var labels = new List<BarcodeLabelData>
        {
            new(
                Barcode: "INT-001",
                BarcodeType: "Internal",
                ProductName: "Simple Product",
                Sku: null,
                Price: null,
                Currency: null)
        };

        var pdf = _sut.GenerateBarcodeLabels(labels, BarcodeLabelSize.Medium);

        pdf.Should().NotBeNullOrEmpty();
        Encoding.ASCII.GetString(pdf, 0, 5).Should().Be("%PDF-");
    }

    [Fact]
    public void GenerateBarcodeLabels_WithNonGelCurrency_ReturnsValidPdf()
    {
        var labels = new List<BarcodeLabelData>
        {
            new(
                Barcode: "4901234567890",
                BarcodeType: "Code128",
                ProductName: "Import Product",
                Sku: "SKU-IMP",
                Price: 25.00m,
                Currency: "USD")
        };

        var pdf = _sut.GenerateBarcodeLabels(labels, BarcodeLabelSize.Large);

        pdf.Should().NotBeNullOrEmpty();
        Encoding.ASCII.GetString(pdf, 0, 5).Should().Be("%PDF-");
    }
}
