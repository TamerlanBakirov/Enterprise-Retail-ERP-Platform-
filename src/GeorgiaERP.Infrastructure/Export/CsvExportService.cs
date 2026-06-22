using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using GeorgiaERP.Application.Common;

namespace GeorgiaERP.Infrastructure.Export;

/// <summary>
/// Export service supporting both CSV and Excel (.xlsx) formats.
/// CSV: UTF-8 with BOM, RFC 4180 escaping.
/// Excel: ClosedXML with styled headers, auto-column-width, and frozen header row.
/// </summary>
public sealed class CsvExportService : IExportService
{
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];

    public byte[] ToCsv<T>(IEnumerable<T> items, IReadOnlyList<ExportColumn<T>> columns)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(columns);

        if (columns.Count == 0)
            return Utf8Bom;

        var sb = new StringBuilder();

        // Header row
        sb.AppendLine(string.Join(",", columns.Select(c => EscapeCsvField(c.Header))));

        // Data rows
        foreach (var item in items)
        {
            var values = new string[columns.Count];
            for (var i = 0; i < columns.Count; i++)
            {
                var column = columns[i];
                var rawValue = column.Selector(item);
                values[i] = EscapeCsvField(FormatValue(rawValue, column.Format));
            }
            sb.AppendLine(string.Join(",", values));
        }

        var csvBytes = Encoding.UTF8.GetBytes(sb.ToString());

        // Prepend UTF-8 BOM for Excel compatibility
        var result = new byte[Utf8Bom.Length + csvBytes.Length];
        Buffer.BlockCopy(Utf8Bom, 0, result, 0, Utf8Bom.Length);
        Buffer.BlockCopy(csvBytes, 0, result, Utf8Bom.Length, csvBytes.Length);

        return result;
    }

    public byte[] ToExcel<T>(IEnumerable<T> items, IReadOnlyList<ExportColumn<T>> columns, string sheetName = "Data")
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(columns);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sheetName);

        // Header row
        for (var col = 0; col < columns.Count; col++)
        {
            var cell = worksheet.Cell(1, col + 1);
            cell.Value = columns[col].Header;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
            cell.Style.Border.BottomBorderColor = XLColor.FromHtml("#2F5496");
        }

        // Data rows
        var row = 2;
        foreach (var item in items)
        {
            for (var col = 0; col < columns.Count; col++)
            {
                var column = columns[col];
                var rawValue = column.Selector(item);
                var cell = worksheet.Cell(row, col + 1);
                SetCellValue(cell, rawValue, column.Format);

                // Alternate row shading for readability
                if (row % 2 == 0)
                {
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#D6E4F0");
                }
            }
            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        // Freeze header row so it stays visible when scrolling
        worksheet.SheetView.FreezeRows(1);

        // Apply auto-filter to header row
        if (columns.Count > 0)
        {
            worksheet.Range(1, 1, Math.Max(row - 1, 1), columns.Count).SetAutoFilter();
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Sets a cell value with proper type handling for Excel.
    /// Numeric values are set as numbers (not text) so Excel formulas work correctly.
    /// </summary>
    private static void SetCellValue(IXLCell cell, object? value, string? format)
    {
        if (value is null)
        {
            cell.SetValue(Blank.Value);
            return;
        }

        switch (value)
        {
            case decimal d:
                cell.SetValue(d);
                if (format is not null) cell.Style.NumberFormat.Format = MapToExcelFormat(format);
                break;
            case double dbl:
                cell.SetValue(dbl);
                if (format is not null) cell.Style.NumberFormat.Format = MapToExcelFormat(format);
                break;
            case float f:
                cell.SetValue((double)f);
                if (format is not null) cell.Style.NumberFormat.Format = MapToExcelFormat(format);
                break;
            case int i:
                cell.SetValue(i);
                break;
            case long l:
                cell.SetValue(l);
                break;
            case DateTimeOffset dto:
                cell.SetValue(dto.DateTime);
                cell.Style.DateFormat.Format = "yyyy-MM-dd HH:mm:ss";
                break;
            case DateTime dt:
                cell.SetValue(dt);
                cell.Style.DateFormat.Format = "yyyy-MM-dd HH:mm:ss";
                break;
            case bool b:
                cell.SetValue(b ? "Yes" : "No");
                break;
            default:
                cell.SetValue(value.ToString() ?? string.Empty);
                break;
        }
    }

    /// <summary>
    /// Maps .NET format strings to Excel number format strings.
    /// </summary>
    private static string MapToExcelFormat(string dotNetFormat)
    {
        return dotNetFormat.ToUpperInvariant() switch
        {
            "N0" => "#,##0",
            "N1" => "#,##0.0",
            "N2" => "#,##0.00",
            "N3" => "#,##0.000",
            "N4" => "#,##0.0000",
            "C0" => "#,##0",
            "C2" => "#,##0.00",
            "P0" => "0%",
            "P2" => "0.00%",
            _ => dotNetFormat
        };
    }

    private static string FormatValue(object? value, string? format)
    {
        if (value is null)
            return string.Empty;

        if (format is not null)
        {
            return value switch
            {
                IFormattable formattable => formattable.ToString(format, CultureInfo.InvariantCulture),
                _ => value.ToString() ?? string.Empty
            };
        }

        return value switch
        {
            DateTimeOffset dto => dto.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            decimal d => d.ToString("G", CultureInfo.InvariantCulture),
            double dbl => dbl.ToString("G", CultureInfo.InvariantCulture),
            float f => f.ToString("G", CultureInfo.InvariantCulture),
            bool b => b ? "Yes" : "No",
            _ => value.ToString() ?? string.Empty
        };
    }

    /// <summary>
    /// Escapes a CSV field per RFC 4180: wraps in double quotes if the value
    /// contains commas, double quotes, or newlines. Double quotes within the
    /// value are doubled.
    /// </summary>
    private static string EscapeCsvField(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
