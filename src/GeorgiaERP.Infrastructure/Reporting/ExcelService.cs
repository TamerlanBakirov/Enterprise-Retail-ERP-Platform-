using ClosedXML.Excel;
using GeorgiaERP.Application.Common;

namespace GeorgiaERP.Infrastructure.Reporting;

public class ExcelService : IExcelService
{
    public byte[] ExportProducts(IReadOnlyList<ProductExportRow> rows)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Products");

        var headers = new[] { "SKU", "Name", "Name (KA)", "Category", "Unit", "VAT Applicable",
            "Weight (kg)", "Min Stock", "Max Stock", "Active", "Created At" };

        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        StyleHeaderRow(ws, headers.Length);

        for (var r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            var xlRow = r + 2;
            ws.Cell(xlRow, 1).Value = row.Sku;
            ws.Cell(xlRow, 2).Value = row.Name;
            ws.Cell(xlRow, 3).Value = row.NameKa ?? "";
            ws.Cell(xlRow, 4).Value = row.CategoryName;
            ws.Cell(xlRow, 5).Value = row.UnitOfMeasure;
            ws.Cell(xlRow, 6).Value = row.VatApplicable ? "Yes" : "No";
            ws.Cell(xlRow, 7).Value = row.WeightKg?.ToString() ?? "";
            ws.Cell(xlRow, 8).Value = row.MinStockLevel?.ToString() ?? "";
            ws.Cell(xlRow, 9).Value = row.MaxStockLevel?.ToString() ?? "";
            ws.Cell(xlRow, 10).Value = row.IsActive ? "Yes" : "No";
            ws.Cell(xlRow, 11).Value = row.CreatedAt.ToString("yyyy-MM-dd HH:mm");
        }

        ws.RangeUsed()?.SetAutoFilter();
        ws.Columns().AdjustToContents();

        return WorkbookToBytes(workbook);
    }

    public byte[] ExportStockLevels(IReadOnlyList<StockExportRow> rows)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Stock Levels");

        var headers = new[] { "SKU", "Product Name", "Warehouse", "Qty On Hand", "Qty Reserved",
            "Available", "Cost Price", "Stock Value", "Low Stock" };

        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        StyleHeaderRow(ws, headers.Length);

        for (var r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            var xlRow = r + 2;
            ws.Cell(xlRow, 1).Value = row.Sku;
            ws.Cell(xlRow, 2).Value = row.ProductName;
            ws.Cell(xlRow, 3).Value = row.WarehouseName;
            ws.Cell(xlRow, 4).Value = row.QuantityOnHand;
            ws.Cell(xlRow, 5).Value = row.QuantityReserved;
            ws.Cell(xlRow, 6).Value = row.Available;
            ws.Cell(xlRow, 7).Value = row.CostPrice;
            ws.Cell(xlRow, 8).Value = row.StockValue;
            ws.Cell(xlRow, 9).Value = row.IsLowStock ? "Yes" : "No";
        }

        ws.RangeUsed()?.SetAutoFilter();
        ws.Columns().AdjustToContents();

        return WorkbookToBytes(workbook);
    }

    public byte[] GenerateProductImportTemplate()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Products");

        var headers = new[] { "SKU*", "Name*", "Name (KA)", "Category Code*", "Unit of Measure*",
            "VAT Applicable", "Weight (kg)", "Min Stock Level", "Max Stock Level" };

        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        StyleHeaderRow(ws, headers.Length);

        ws.Cell(2, 1).Value = "PROD-001";
        ws.Cell(2, 2).Value = "Sample Product";
        ws.Cell(2, 3).Value = "";
        ws.Cell(2, 4).Value = "CAT-01";
        ws.Cell(2, 5).Value = "pcs";
        ws.Cell(2, 6).Value = "true";
        ws.Cell(2, 7).Value = "0.5";
        ws.Cell(2, 8).Value = "10";
        ws.Cell(2, 9).Value = "100";

        var notesWs = workbook.Worksheets.Add("Notes");
        notesWs.Cell(1, 1).Value = "Field";
        notesWs.Cell(1, 2).Value = "Description";
        StyleHeaderRow(notesWs, 2);

        notesWs.Cell(2, 1).Value = "SKU*";
        notesWs.Cell(2, 2).Value = "Required. Unique product identifier.";
        notesWs.Cell(3, 1).Value = "Name*";
        notesWs.Cell(3, 2).Value = "Required. Product name in default language.";
        notesWs.Cell(4, 1).Value = "Name (KA)";
        notesWs.Cell(4, 2).Value = "Optional. Product name in Georgian.";
        notesWs.Cell(5, 1).Value = "Category Code*";
        notesWs.Cell(5, 2).Value = "Required. Must match an existing category code.";
        notesWs.Cell(6, 1).Value = "Unit of Measure*";
        notesWs.Cell(6, 2).Value = "Required. E.g. pcs, kg, l, m.";
        notesWs.Cell(7, 1).Value = "VAT Applicable";
        notesWs.Cell(7, 2).Value = "true or false. Defaults to true if empty.";
        notesWs.Cell(8, 1).Value = "Weight (kg)";
        notesWs.Cell(8, 2).Value = "Optional. Numeric value.";
        notesWs.Cell(9, 1).Value = "Min Stock Level";
        notesWs.Cell(9, 2).Value = "Optional. Numeric value for low-stock alerts.";
        notesWs.Cell(10, 1).Value = "Max Stock Level";
        notesWs.Cell(10, 2).Value = "Optional. Numeric value for maximum stock.";

        notesWs.Columns().AdjustToContents();
        ws.Columns().AdjustToContents();

        return WorkbookToBytes(workbook);
    }

    public Result<List<ProductImportRow>> ParseProductImport(Stream fileStream)
    {
        using var workbook = new XLWorkbook(fileStream);
        var ws = workbook.Worksheets.First();

        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
        if (lastRow <= 1)
            return Result.Success(new List<ProductImportRow>());

        var rows = new List<ProductImportRow>();
        var errors = new List<string>();

        for (var r = 2; r <= lastRow; r++)
        {
            var sku = ws.Cell(r, 1).GetString().Trim();
            var name = ws.Cell(r, 2).GetString().Trim();
            var nameKa = ws.Cell(r, 3).GetString().Trim();
            var categoryCode = ws.Cell(r, 4).GetString().Trim();
            var unitOfMeasure = ws.Cell(r, 5).GetString().Trim();
            var vatStr = ws.Cell(r, 6).GetString().Trim();
            var weightStr = ws.Cell(r, 7).GetString().Trim();
            var minStockStr = ws.Cell(r, 8).GetString().Trim();
            var maxStockStr = ws.Cell(r, 9).GetString().Trim();

            if (string.IsNullOrWhiteSpace(sku) && string.IsNullOrWhiteSpace(name))
                continue;

            var rowErrors = new List<string>();
            if (string.IsNullOrWhiteSpace(sku)) rowErrors.Add("SKU is required");
            if (string.IsNullOrWhiteSpace(name)) rowErrors.Add("Name is required");
            if (string.IsNullOrWhiteSpace(categoryCode)) rowErrors.Add("Category Code is required");
            if (string.IsNullOrWhiteSpace(unitOfMeasure)) rowErrors.Add("Unit of Measure is required");

            if (rowErrors.Count > 0)
            {
                errors.Add($"Row {r}: {string.Join("; ", rowErrors)}");
                continue;
            }

            var vatApplicable = !bool.TryParse(vatStr, out var vat) || vat;
            decimal? weightKg = decimal.TryParse(weightStr, out var w) ? w : null;
            decimal? minStock = decimal.TryParse(minStockStr, out var min) ? min : null;
            decimal? maxStock = decimal.TryParse(maxStockStr, out var max) ? max : null;

            rows.Add(new ProductImportRow(
                r, sku, name,
                string.IsNullOrWhiteSpace(nameKa) ? null : nameKa,
                categoryCode, unitOfMeasure, vatApplicable,
                weightKg, minStock, maxStock));
        }

        if (errors.Count > 0)
            return Result.ValidationFailure<List<ProductImportRow>>(errors);

        return Result.Success(rows);
    }

    private static void StyleHeaderRow(IXLWorksheet ws, int columnCount)
    {
        var headerRange = ws.Range(1, 1, 1, columnCount);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(0x4472C4);
        headerRange.Style.Font.FontColor = XLColor.White;
    }

    private static byte[] WorkbookToBytes(XLWorkbook workbook)
    {
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }
}
