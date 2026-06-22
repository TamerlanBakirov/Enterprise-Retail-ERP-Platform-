using GeorgiaERP.Application.Common;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GeorgiaERP.Infrastructure.Reporting;

public class PdfGenerationService : IPdfGenerationService
{
    private const float ReceiptWidthPt = 227f;
    private const string CurrencySymbol = "₾";

    public PdfGenerationService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateReceipt(ReceiptData data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(new PageSize(ReceiptWidthPt, 1000f));
                page.MarginVertical(10, Unit.Point);
                page.MarginHorizontal(8, Unit.Point);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Content().Column(col =>
                {
                    col.Spacing(4);

                    if (data.CompanyName is not null)
                    {
                        col.Item().AlignCenter().Text(data.CompanyName).Bold().FontSize(10);
                    }

                    if (data.CompanyTin is not null)
                    {
                        col.Item().AlignCenter().Text($"TIN: {data.CompanyTin}").FontSize(7);
                    }

                    col.Item().AlignCenter().Text(data.StoreName).Bold().FontSize(9);

                    if (data.StoreAddress is not null)
                    {
                        col.Item().AlignCenter().Text(data.StoreAddress).FontSize(7);
                    }

                    col.Item().LineHorizontal(0.5f);

                    col.Item().Text($"Receipt: {data.TransactionNumber}").FontSize(7);
                    col.Item().Text($"Date: {data.Date:dd.MM.yyyy HH:mm:ss}").FontSize(7);

                    if (data.CashierName is not null)
                    {
                        col.Item().Text($"Cashier: {data.CashierName}").FontSize(7);
                    }

                    if (data.TerminalId is not null)
                    {
                        col.Item().Text($"Terminal: {data.TerminalId}").FontSize(7);
                    }

                    col.Item().LineHorizontal(0.5f);

                    foreach (var line in data.Lines)
                    {
                        col.Item().Text(line.ProductName).FontSize(8);
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"  {line.Quantity} {line.Unit} x {line.UnitPrice:N2}").FontSize(7);
                            row.ConstantItem(60, Unit.Point).AlignRight()
                                .Text($"{CurrencySymbol}{line.Total:N2}").FontSize(7);
                        });

                        if (line.Discount > 0)
                        {
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("  Discount").FontSize(7);
                                row.ConstantItem(60, Unit.Point).AlignRight()
                                    .Text($"-{CurrencySymbol}{line.Discount:N2}").FontSize(7);
                            });
                        }
                    }

                    col.Item().LineHorizontal(0.5f);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Subtotal");
                        row.ConstantItem(70, Unit.Point).AlignRight()
                            .Text($"{CurrencySymbol}{data.Subtotal:N2}");
                    });

                    if (data.DiscountTotal > 0)
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Discount");
                            row.ConstantItem(70, Unit.Point).AlignRight()
                                .Text($"-{CurrencySymbol}{data.DiscountTotal:N2}");
                        });
                    }

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("VAT (18%)");
                        row.ConstantItem(70, Unit.Point).AlignRight()
                            .Text($"{CurrencySymbol}{data.VatTotal:N2}");
                    });

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text(text => text.Span("TOTAL").Bold().FontSize(10));
                        row.ConstantItem(70, Unit.Point).AlignRight()
                            .Text(text => text.Span($"{CurrencySymbol}{data.Total:N2}").Bold().FontSize(10));
                    });

                    col.Item().LineHorizontal(0.5f);

                    foreach (var payment in data.Payments)
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text(payment.Method).FontSize(7);
                            row.ConstantItem(70, Unit.Point).AlignRight()
                                .Text($"{CurrencySymbol}{payment.Amount:N2}").FontSize(7);
                        });

                        if (payment.Change is > 0)
                        {
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("  Change").FontSize(7);
                                row.ConstantItem(70, Unit.Point).AlignRight()
                                    .Text($"{CurrencySymbol}{payment.Change:N2}").FontSize(7);
                            });
                        }
                    }

                    if (data.FiscalReceiptId is not null)
                    {
                        col.Item().PaddingTop(4).AlignCenter()
                            .Text($"Fiscal ID: {data.FiscalReceiptId}").FontSize(7).Bold();
                    }

                    if (data.CustomerName is not null)
                    {
                        col.Item().Text($"Customer: {data.CustomerName}").FontSize(7);
                    }

                    if (data.LoyaltyPointsEarned is > 0)
                    {
                        col.Item().Text($"Loyalty points earned: {data.LoyaltyPointsEarned}").FontSize(7);
                    }

                    col.Item().PaddingTop(6).AlignCenter()
                        .Text("Thank you for your purchase!").FontSize(7).Italic();
                });
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateInvoice(InvoiceData data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginTop(30, Unit.Point);
                page.MarginBottom(30, Unit.Point);
                page.MarginHorizontal(40, Unit.Point);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            if (data.SellerNameKa is not null)
                            {
                                left.Item().Text(data.SellerNameKa).Bold().FontSize(14);
                            }

                            left.Item().Text(data.SellerName).Bold().FontSize(data.SellerNameKa is not null ? 10 : 14);
                        });

                        row.RelativeItem().AlignRight().Column(right =>
                        {
                            right.Item().AlignRight().Text("INVOICE / ფაქტურა")
                                .Bold().FontSize(16);
                            right.Item().AlignRight().Text($"# {data.InvoiceNumber}").FontSize(11);
                        });
                    });

                    col.Item().PaddingTop(10).LineHorizontal(1);
                });

                page.Content().PaddingTop(15).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(seller =>
                        {
                            seller.Item().Text("Seller / გამყიდველი")
                                .Bold().FontSize(10);
                            seller.Item().Text(data.SellerName);
                            seller.Item().Text($"TIN: {data.SellerTin}");

                            if (data.SellerIsVatPayer)
                                seller.Item().Text("VAT Payer").FontSize(8).Italic();

                            if (data.SellerAddress is not null)
                                seller.Item().Text(data.SellerAddress);

                            if (data.SellerPhone is not null)
                                seller.Item().Text($"Tel: {data.SellerPhone}");

                            if (data.SellerEmail is not null)
                                seller.Item().Text($"Email: {data.SellerEmail}");
                        });

                        row.ConstantItem(20);

                        row.RelativeItem().Column(buyer =>
                        {
                            buyer.Item().Text("Buyer / მყიდველი")
                                .Bold().FontSize(10);

                            if (data.BuyerName is not null)
                                buyer.Item().Text(data.BuyerName);

                            if (data.BuyerTin is not null)
                                buyer.Item().Text($"TIN: {data.BuyerTin}");

                            if (data.BuyerAddress is not null)
                                buyer.Item().Text(data.BuyerAddress);
                        });

                        row.ConstantItem(20);

                        row.RelativeItem().Column(details =>
                        {
                            details.Item().Text("Details").Bold().FontSize(10);
                            details.Item().Text($"Date: {data.Date:dd.MM.yyyy}");

                            if (data.DueDate.HasValue)
                                details.Item().Text($"Due: {data.DueDate:dd.MM.yyyy}");

                            details.Item().Text($"Currency: {data.Currency} ({CurrencySymbol})");
                        });
                    });

                    col.Item().PaddingTop(15).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30);
                            columns.RelativeColumn(4);
                            columns.ConstantColumn(45);
                            columns.ConstantColumn(55);
                            columns.ConstantColumn(65);
                            columns.ConstantColumn(55);
                            columns.ConstantColumn(70);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4)
                                .Text("#").Bold().FontSize(8);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4)
                                .Text("Description").Bold().FontSize(8);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4)
                                .Text("Unit").Bold().FontSize(8);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight()
                                .Text("Qty").Bold().FontSize(8);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight()
                                .Text("Price").Bold().FontSize(8);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight()
                                .Text("VAT").Bold().FontSize(8);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight()
                                .Text("Total").Bold().FontSize(8);
                        });

                        foreach (var line in data.Lines)
                        {
                            var bgColor = line.LineNumber % 2 == 0 ? Colors.Grey.Lighten5 : Colors.White;

                            table.Cell().Background(bgColor).Padding(3)
                                .Text(line.LineNumber.ToString()).FontSize(8);
                            table.Cell().Background(bgColor).Padding(3)
                                .Text(line.ProductName).FontSize(8);
                            table.Cell().Background(bgColor).Padding(3)
                                .Text(line.Unit).FontSize(8);
                            table.Cell().Background(bgColor).Padding(3).AlignRight()
                                .Text(line.Quantity.ToString("N2")).FontSize(8);
                            table.Cell().Background(bgColor).Padding(3).AlignRight()
                                .Text($"{CurrencySymbol}{line.UnitPrice:N2}").FontSize(8);
                            table.Cell().Background(bgColor).Padding(3).AlignRight()
                                .Text($"{CurrencySymbol}{line.VatAmount:N2}").FontSize(8);
                            table.Cell().Background(bgColor).Padding(3).AlignRight()
                                .Text($"{CurrencySymbol}{line.Total:N2}").FontSize(8);
                        }
                    });

                    col.Item().PaddingTop(10).AlignRight().Width(200).Column(totals =>
                    {
                        totals.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Subtotal:");
                            row.ConstantItem(80).AlignRight()
                                .Text($"{CurrencySymbol}{data.Subtotal:N2}");
                        });

                        totals.Item().Row(row =>
                        {
                            row.RelativeItem().Text("VAT (18%):");
                            row.ConstantItem(80).AlignRight()
                                .Text($"{CurrencySymbol}{data.VatTotal:N2}");
                        });

                        totals.Item().PaddingTop(4).BorderTop(1).PaddingTop(4).Row(row =>
                        {
                            row.RelativeItem()
                                .Text(text => text.Span("TOTAL:").Bold().FontSize(11));
                            row.ConstantItem(80).AlignRight()
                                .Text(text => text.Span($"{CurrencySymbol}{data.Total:N2}").Bold().FontSize(11));
                        });
                    });

                    if (data.BankName is not null || data.BankAccount is not null)
                    {
                        col.Item().PaddingTop(20).Column(bank =>
                        {
                            bank.Item().Text("Bank Details / საბანკო რეკვიზიტები")
                                .Bold().FontSize(10);

                            if (data.BankName is not null)
                                bank.Item().Text($"Bank: {data.BankName}");

                            if (data.BankAccount is not null)
                                bank.Item().Text($"Account: {data.BankAccount}");
                        });
                    }

                    if (data.Notes is not null)
                    {
                        col.Item().PaddingTop(15).Column(notes =>
                        {
                            notes.Item().Text("Notes").Bold().FontSize(10);
                            notes.Item().Text(data.Notes).FontSize(8);
                        });
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }
}
