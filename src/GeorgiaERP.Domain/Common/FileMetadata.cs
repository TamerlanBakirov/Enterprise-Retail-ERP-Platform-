using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Common;

/// <summary>
/// Stores metadata about uploaded files (product images, document attachments).
/// The actual file content is stored on disk via IFileStorageService.
/// </summary>
public class FileMetadata : AuditableEntity
{
    public string FileName { get; private set; } = default!;
    public string StoredFileName { get; private set; } = default!;
    public string ContentType { get; private set; } = default!;
    public long SizeBytes { get; private set; }
    public string? Category { get; private set; }
    public Guid? EntityId { get; private set; }
    public string? EntityType { get; private set; }
    public Guid UploadedBy { get; private set; }
    public DateTimeOffset UploadedAt { get; private set; }

    private FileMetadata() { }

    public static FileMetadata Create(
        string fileName,
        string storedFileName,
        string contentType,
        long sizeBytes,
        Guid uploadedBy,
        string? category = null,
        Guid? entityId = null,
        string? entityType = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(storedFileName))
            throw new ArgumentException("Stored file name is required.", nameof(storedFileName));
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Content type is required.", nameof(contentType));
        if (sizeBytes <= 0)
            throw new ArgumentException("File size must be positive.", nameof(sizeBytes));
        if (uploadedBy == Guid.Empty)
            throw new ArgumentException("Uploader ID is required.", nameof(uploadedBy));

        return new FileMetadata
        {
            FileName = fileName,
            StoredFileName = storedFileName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            UploadedBy = uploadedBy,
            UploadedAt = DateTimeOffset.UtcNow,
            Category = category,
            EntityId = entityId,
            EntityType = entityType
        };
    }

    public void LinkToEntity(Guid entityId, string entityType)
    {
        EntityId = entityId;
        EntityType = entityType;
    }
}
