namespace GeorgiaERP.Application.Common;

/// <summary>
/// Service for importing data from CSV or Excel files.
/// Supports row-level validation and returns a detailed error report for invalid rows.
/// </summary>
public interface IImportService
{
    /// <summary>
    /// Parses rows from a CSV or Excel file stream.
    /// Automatically detects format based on content type.
    /// </summary>
    /// <param name="stream">The file stream.</param>
    /// <param name="contentType">The MIME content type (text/csv or application/vnd.openxmlformats-...).</param>
    /// <returns>List of dictionaries, one per row, keyed by column header.</returns>
    List<Dictionary<string, string>> ParseRows(Stream stream, string contentType);
}

/// <summary>
/// Result of a data import operation.
/// </summary>
public sealed class ImportResult
{
    public int TotalRows { get; init; }
    public int SuccessCount { get; init; }
    public int ErrorCount { get; init; }
    public IReadOnlyList<ImportRowError> Errors { get; init; } = [];
    public bool IsSuccess => ErrorCount == 0;
}

/// <summary>
/// Details about a failed import row.
/// </summary>
public sealed class ImportRowError
{
    public int RowNumber { get; init; }
    public string Field { get; init; } = default!;
    public string Error { get; init; } = default!;
    public string? Value { get; init; }
}
