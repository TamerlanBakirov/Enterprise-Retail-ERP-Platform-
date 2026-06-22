namespace GeorgiaERP.Application.Common;

/// <summary>
/// Abstraction for file storage operations. Implementations may store files
/// on local disk, cloud blob storage, or other backends.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Saves a file to storage and returns the stored file name (unique key).
    /// </summary>
    Task<string> SaveAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a file from storage by its stored file name.
    /// Returns null if the file does not exist.
    /// </summary>
    Task<FileStorageResult?> GetAsync(string storedFileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from storage by its stored file name.
    /// Returns true if the file was deleted, false if it didn't exist.
    /// </summary>
    Task<bool> DeleteAsync(string storedFileName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of retrieving a file from storage.
/// </summary>
public record FileStorageResult(Stream Content, string ContentType, string FileName);
