using System.Text;
using FluentAssertions;
using GeorgiaERP.Application.Common;
using GeorgiaERP.Infrastructure.Reporting;
using Xunit;

namespace GeorgiaERP.Tests.Infrastructure;

public class PdfGenerationTests
{
    private readonly PdfGenerationService _sut = new();

    private static ReceiptData SampleReceipt(List<ReceiptLineData>? lines = null) => new()
    {
        TransactionNumber = "TX-TEST-001",
        Date = new DateTimeOffset(2024, 6, 15, 14, 30, 0, TimeSpan.FromHours(4)),
        StoreName = "Test Store",
        StoreAddress = "1 Rustaveli Ave, Tbilisi",
        CompanyName = "Test Company LLC",
        CompanyTin = "123456789",
        CashierName = "Nino Kapanadze",
        TerminalId = "T-001",
        Lines = lines ?? [
            new ReceiptLineData("Coca-Cola 0.5L", 2m, "pcs", 3.50m, 0m, 7.00m),
            new ReceiptLineData("Bread", 1m, "pcs", 1.80m, 0.20m, 1.60m)
        ],
        Subtotal = 8.80m,
        DiscountTotal = 0.20m,
        VatTotal = 1.35m,
        Total = 8.60m,
        Payments = [new ReceiptPaymentData("Cash", 10.00m, 1.40m)],
        FiscalReceiptId = "FISCAL-2024-001",
        CustomerName = "Giorgi Beridze",
        LoyaltyPointsEarned = 8
    };

    private static InvoiceData SampleInvoice(List<InvoiceLineData>? lines = null) => new()
    {
        InvoiceNumber = "INV-2024-001",
        Date = new DateTimeOffset(2024, 6, 15, 14, 30, 0, TimeSpan.FromHours(4)),
        DueDate = new DateTimeOffset(2024, 7, 15, 0, 0, 0, TimeSpan.FromHours(4)),
        SellerName = "Georgian Retail LLC",
        SellerNameKa = "ქართული რითეილი",
        SellerTin = "987654321",
        SellerAddress = "10 Chavchavadze Ave, Tbilisi",
        SellerPhone = "+995 555 123456",
        SellerEmail = "info@georgianretail.ge",
        SellerIsVatPayer = true,
        BuyerName = "Buyer Company Ltd",
        BuyerTin = "111222333",
        BuyerAddress = "5 Tsereteli Ave, Kutaisi",
        Lines = lines ?? [
            new InvoiceLineData(1, "Office Chair", "pcs", 5m, 250.00m, 225.00m, 1475.00m),
            new InvoiceLineData(2, "Office Desk", "pcs", 3m, 400.00m, 216.00m, 1416.00m)
        ],
        Subtotal = 2450.00m,
        VatTotal = 441.00m,
        Total = 2891.00m,
        Notes = "Payment due within 30 days",
        BankName = "TBC Bank",
        BankAccount = "GE29TB7894545082100004",
        Currency = "GEL"
    };

    [Fact]
    public void GenerateReceipt_ReturnsValidPdfBytes()
    {
        var pdf = _sut.GenerateReceipt(SampleReceipt());

        pdf.Should().NotBeNullOrEmpty();
        pdf.Length.Should().BeGreaterThan(100);
        Encoding.ASCII.GetString(pdf, 0, 5).Should().Be("%PDF-");
    }

    [Fact]
    public void GenerateInvoice_ReturnsValidPdfBytes()
    {
        var pdf = _sut.GenerateInvoice(SampleInvoice());

        pdf.Should().NotBeNullOrEmpty();
        pdf.Length.Should().BeGreaterThan(100);
        Encoding.ASCII.GetString(pdf, 0, 5).Should().Be("%PDF-");
    }

    [Fact]
    public void GenerateReceipt_WithEmptyLines_ReturnsValidPdf()
    {
        var pdf = _sut.GenerateReceipt(SampleReceipt(lines: []));

        pdf.Should().NotBeNullOrEmpty();
        Encoding.ASCII.GetString(pdf, 0, 5).Should().Be("%PDF-");
    }

    [Fact]
    public void GenerateInvoice_WithEmptyLines_ReturnsValidPdf()
    {
        var pdf = _sut.GenerateInvoice(SampleInvoice(lines: []));

        pdf.Should().NotBeNullOrEmpty();
        Encoding.ASCII.GetString(pdf, 0, 5).Should().Be("%PDF-");
    }

    [Fact]
    public void GenerateReceipt_WithMultiplePayments_ReturnsValidPdf()
    {
        var data = SampleReceipt() with
        {
            Payments =
            [
                new ReceiptPaymentData("Cash", 5.00m, 0m),
                new ReceiptPaymentData("Card", 3.60m, null)
            ]
        };

        var pdf = _sut.GenerateReceipt(data);

        pdf.Should().NotBeNullOrEmpty();
        Encoding.ASCII.GetString(pdf, 0, 5).Should().Be("%PDF-");
    }

    [Fact]
    public void GenerateInvoice_WithBankDetails_ProducesLargerPdf()
    {
        var withBank = SampleInvoice();
        var withoutBank = SampleInvoice() with { BankName = null, BankAccount = null, Notes = null };

        var pdfWithBank = _sut.GenerateInvoice(withBank);
        var pdfWithoutBank = _sut.GenerateInvoice(withoutBank);

        pdfWithBank.Length.Should().BeGreaterThan(pdfWithoutBank.Length);
    }

    [Fact]
    public void GenerateReceipt_AndInvoice_ProduceDifferentSizedPdfs()
    {
        var receipt = _sut.GenerateReceipt(SampleReceipt());
        var invoice = _sut.GenerateInvoice(SampleInvoice());

        receipt.Length.Should().NotBe(invoice.Length);
    }
}
