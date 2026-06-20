using System.IO;
using System.Text;
using GeorgiaERP.Desktop.Models;
using Microsoft.Win32;

namespace GeorgiaERP.Desktop.Services;

public static class CsvExportService
{
    public static bool ExportSalesReport(SalesReportDto report, string defaultFileName = "sales_report")
    {
        var sb = new StringBuilder();
        sb.AppendLine("Sales Report Summary");
        sb.AppendLine($"Total Revenue,{report.TotalRevenue:F2}");
        sb.AppendLine($"Total VAT,{report.TotalVat:F2}");
        sb.AppendLine($"Transaction Count,{report.TransactionCount}");
        sb.AppendLine($"Average Transaction,{report.AverageTransactionValue:F2}");
        sb.AppendLine();

        if (report.DailySummary.Count > 0)
        {
            sb.AppendLine("Daily Summary");
            sb.AppendLine("Date,Revenue,Transactions");
            foreach (var d in report.DailySummary)
                sb.AppendLine($"{d.Date:yyyy-MM-dd},{d.Revenue:F2},{d.TransactionCount}");
            sb.AppendLine();
        }

        if (report.PaymentBreakdown.Count > 0)
        {
            sb.AppendLine("Payment Methods");
            sb.AppendLine("Method,Amount,Count");
            foreach (var p in report.PaymentBreakdown)
                sb.AppendLine($"{Escape(p.PaymentMethod)},{p.Amount:F2},{p.Count}");
            sb.AppendLine();
        }

        if (report.TopItems.Count > 0)
        {
            sb.AppendLine("Top Selling Items");
            sb.AppendLine("Product,Quantity,Revenue");
            foreach (var t in report.TopItems)
                sb.AppendLine($"{Escape(t.ProductName)},{t.Quantity:F2},{t.Revenue:F2}");
        }

        return SaveFile(sb, defaultFileName);
    }

    public static bool ExportStockReport(StockReportDto report, string defaultFileName = "stock_report")
    {
        var sb = new StringBuilder();
        sb.AppendLine("Stock Report Summary");
        sb.AppendLine($"Total Stock Value,{report.TotalStockValue:F2}");
        sb.AppendLine($"Total Products,{report.TotalProducts}");
        sb.AppendLine($"Low Stock,{report.LowStockCount}");
        sb.AppendLine($"Out of Stock,{report.OutOfStockCount}");
        sb.AppendLine();

        if (report.StockLevels.Count > 0)
        {
            sb.AppendLine("Stock Levels");
            sb.AppendLine("SKU,Product,Warehouse,Quantity,Reorder Point,Low Stock");
            foreach (var s in report.StockLevels)
                sb.AppendLine($"{Escape(s.Sku)},{Escape(s.ProductName)},{Escape(s.WarehouseName)},{s.Quantity:F2},{s.ReorderPoint:F0},{s.IsLowStock}");
        }

        return SaveFile(sb, defaultFileName);
    }

    public static bool ExportVatReport(VatReportDto report, string defaultFileName = "vat_report")
    {
        var sb = new StringBuilder();
        sb.AppendLine("VAT Report");
        sb.AppendLine($"Period,{report.Year}-{report.Month:D2}");
        sb.AppendLine($"Sales VAT,{report.TotalSalesVat:F2}");
        sb.AppendLine($"Purchase VAT,{report.TotalPurchaseVat:F2}");
        sb.AppendLine($"Net VAT Payable,{report.NetVat:F2}");
        sb.AppendLine($"Total Fiscal Documents,{report.TotalFiscalDocuments}");
        sb.AppendLine($"Submitted,{report.SubmittedDocuments}");
        sb.AppendLine($"Pending,{report.PendingDocuments}");
        sb.AppendLine($"Failed,{report.FailedDocuments}");

        return SaveFile(sb, defaultFileName);
    }

    private static bool SaveFile(StringBuilder content, string defaultFileName)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "CSV Files (*.csv)|*.csv",
            FileName = $"{defaultFileName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
            DefaultExt = ".csv"
        };

        if (dialog.ShowDialog() != true) return false;

        File.WriteAllText(dialog.FileName, content.ToString(), Encoding.UTF8);
        return true;
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
