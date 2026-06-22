namespace GeorgiaERP.Application.Common;

/// <summary>
/// Service for exporting data to various formats (CSV, etc.).
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Exports a collection of items to CSV format.
    /// </summary>
    /// <typeparam name="T">The type of items to export.</typeparam>
    /// <param name="items">The items to export.</param>
    /// <param name="columns">Column definitions specifying header, selector, and optional format.</param>
    /// <returns>UTF-8 encoded CSV bytes (includes BOM for Excel compatibility).</returns>
    byte[] ToCsv<T>(IEnumerable<T> items, IReadOnlyList<ExportColumn<T>> columns);
}

/// <summary>
/// Defines a column for data export.
/// </summary>
/// <typeparam name="T">The type of source items.</typeparam>
public sealed class ExportColumn<T>
{
    /// <summary>
    /// Column header text.
    /// </summary>
    public required string Header { get; init; }

    /// <summary>
    /// Function to extract the cell value from a source item.
    /// </summary>
    public required Func<T, object?> Selector { get; init; }

    /// <summary>
    /// Optional format string for the value (e.g., "N2" for decimals, "yyyy-MM-dd" for dates).
    /// </summary>
    public string? Format { get; init; }
}
