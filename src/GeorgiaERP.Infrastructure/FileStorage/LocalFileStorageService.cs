using GeorgiaERP.Application.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GeorgiaERP.Infrastructure.FileStorage;

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>
    /// Base directory path for file storage. Defaults to "./uploads".
    /// </summary>
    public string BasePath { get; set; } = "./uploads";

    /// <summary>
    /// Maximum allowed file size in bytes. Defaults to 10 MB.
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// Allowed file extensions (lowercase, with dot). Empty = allow all.
    /// </summary>
    public string[] AllowedExtensions { get; set; } =
    [
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".csv", ".txt"
    ];

    /// <summary>
    /// Allowed MIME types. Empty = allow all.
    /// </summary>
    public string[] AllowedContentTypes { get; set; } =
    [
        "image/jpeg", "image/png", "image/gif", "image/webp", "image/bmp",
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "text/csv", "text/plain"
    ];
}

/// <summary>
/// Stores files on the local disk filesystem, organized by date-based subdirectories.
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly FileStorageOptions _options;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(
        IOptions<FileStorageOptions> options,
        ILogger<LocalFileStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> SaveAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        // Generate a unique stored file name with date-based directory
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var dateDir = DateTime.UtcNow.ToString("yyyy/MM/dd");
        var uniqueName = $"{Guid.NewGuid():N}{extension}";
        var relativePath = Path.Combine(dateDir, uniqueName).Replace('\\', '/');

        var fullDir = Path.Combine(_options.BasePath, dateDir);
        Directory.CreateDirectory(fullDir);

        var fullPath = Path.Combine(_options.BasePath, relativePath.Replace('/', Path.DirectorySeparatorChar));

        await using var fileStreamOut = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
        await fileStream.CopyToAsync(fileStreamOut, cancellationToken);

        _logger.LogInformation("File saved: {StoredFileName} ({SizeBytes} bytes, {ContentType})",
            relativePath, fileStreamOut.Length, contentType);

        return relativePath;
    }

    public Task<FileStorageResult?> GetAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_options.BasePath, storedFileName.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("File not found on disk: {StoredFileName}", storedFileName);
            return Task.FromResult<FileStorageResult?>(null);
        }

        // Determine content type from extension
        var extension = Path.GetExtension(fullPath).ToLowerInvariant();
        var contentType = GetContentType(extension);

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        var originalFileName = Path.GetFileName(storedFileName);

        return Task.FromResult<FileStorageResult?>(new FileStorageResult(stream, contentType, originalFileName));
    }

    public Task<bool> DeleteAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_options.BasePath, storedFileName.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(fullPath))
            return Task.FromResult(false);

        File.Delete(fullPath);
        _logger.LogInformation("File deleted: {StoredFileName}", storedFileName);
        return Task.FromResult(true);
    }

    private static string GetContentType(string extension) => extension switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        ".bmp" => "image/bmp",
        ".pdf" => "application/pdf",
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xls" => "application/vnd.ms-excel",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ".csv" => "text/csv",
        ".txt" => "text/plain",
        _ => "application/octet-stream"
    };
}
