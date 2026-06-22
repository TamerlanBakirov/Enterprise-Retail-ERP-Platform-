namespace GeorgiaERP.Application.Files.DTOs;

public record FileMetadataDto(
    Guid Id,
    string FileName,
    string StoredFileName,
    string ContentType,
    long SizeBytes,
    string? Category,
    Guid? EntityId,
    string? EntityType,
    Guid UploadedBy,
    DateTimeOffset UploadedAt);
