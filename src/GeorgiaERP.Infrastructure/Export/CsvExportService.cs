using System.Globalization;
using System.Text;
using GeorgiaERP.Application.Common;

namespace GeorgiaERP.Infrastructure.Export;

/// <summary>
/// CSV export implementation using UTF-8 with BOM for Excel compatibility.
/// Fields containing commas, quotes, or newlines are properly escaped per RFC 4180.
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
