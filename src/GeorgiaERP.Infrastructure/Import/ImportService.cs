using System.Globalization;
using ClosedXML.Excel;
using GeorgiaERP.Application.Common;

namespace GeorgiaERP.Infrastructure.Import;

/// <summary>
/// Parses CSV and Excel files into a list of row dictionaries.
/// Each row is a dictionary keyed by the column header (case-insensitive trimmed).
/// </summary>
public sealed class ImportService : IImportService
{
    public List<Dictionary<string, string>> ParseRows(Stream stream, string contentType)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (IsExcel(contentType))
            return ParseExcel(stream);

        return ParseCsv(stream);
    }

    private static bool IsExcel(string contentType)
    {
        return contentType.Contains("spreadsheet", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("xlsx", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("excel", StringComparison.OrdinalIgnoreCase);
    }

    private static List<Dictionary<string, string>> ParseExcel(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();
        var rows = new List<Dictionary<string, string>>();

        var headerRow = worksheet.Row(1);
        var headers = new List<string>();
        var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

        for (var col = 1; col <= lastColumn; col++)
        {
            var header = headerRow.Cell(col).GetString().Trim();
            headers.Add(header);
        }

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        for (var row = 2; row <= lastRow; row++)
        {
            var wsRow = worksheet.Row(row);

            // Skip completely empty rows
            if (wsRow.IsEmpty())
                continue;

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var col = 0; col < headers.Count; col++)
            {
                var cell = wsRow.Cell(col + 1);
                var value = cell.DataType == XLDataType.Number
                    ? cell.GetDouble().ToString(CultureInfo.InvariantCulture)
                    : cell.GetString().Trim();
                dict[headers[col]] = value;
            }
            rows.Add(dict);
        }

        return rows;
    }

    private static List<Dictionary<string, string>> ParseCsv(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var rows = new List<Dictionary<string, string>>();

        var headerLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(headerLine))
            return rows;

        // Strip UTF-8 BOM if present
        if (headerLine.Length > 0 && headerLine[0] == '﻿')
            headerLine = headerLine[1..];

        var headers = ParseCsvLine(headerLine);

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var values = ParseCsvLine(line);
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Count; i++)
            {
                dict[headers[i]] = i < values.Count ? values[i] : string.Empty;
            }
            rows.Add(dict);
        }

        return rows;
    }

    /// <summary>
    /// Parses a single CSV line respecting RFC 4180 quoting rules.
    /// </summary>
    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var inQuotes = false;
        var field = new System.Text.StringBuilder();

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    // Check for escaped quote ("")
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        field.Append('"');
                        i++; // Skip next quote
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    fields.Add(field.ToString().Trim());
                    field.Clear();
                }
                else
                {
                    field.Append(c);
                }
            }
        }

        fields.Add(field.ToString().Trim());
        return fields;
    }
}
