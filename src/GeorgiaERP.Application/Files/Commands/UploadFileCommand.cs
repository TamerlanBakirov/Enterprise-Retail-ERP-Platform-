using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Files.DTOs;
using GeorgiaERP.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Files.Commands;

public record UploadFileCommand(
    Stream FileStream,
    string FileName,
    string ContentType,
    long SizeBytes,
    Guid UploadedBy,
    string? Category = null,
    Guid? EntityId = null,
    string? EntityType = null) : IRequest<Result<FileMetadataDto>>;

public class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, Result<FileMetadataDto>>
{
    private readonly IAppDbContext _db;
    private readonly IFileStorageService _fileStorage;

    public UploadFileCommandHandler(IAppDbContext db, IFileStorageService fileStorage)
    {
        _db = db;
        _fileStorage = fileStorage;
    }

    public async Task<Result<FileMetadataDto>> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        // Save file to storage
        var storedFileName = await _fileStorage.SaveAsync(
            request.FileStream, request.FileName, request.ContentType, cancellationToken);

        // Create metadata entity
        var metadata = FileMetadata.Create(
            request.FileName,
            storedFileName,
            request.ContentType,
            request.SizeBytes,
            request.UploadedBy,
            request.Category,
            request.EntityId,
            request.EntityType);

        _db.FileMetadata.Add(metadata);
        await _db.SaveChangesAsync(cancellationToken);

        var dto = new FileMetadataDto(
            metadata.Id,
            metadata.FileName,
            metadata.StoredFileName,
            metadata.ContentType,
            metadata.SizeBytes,
            metadata.Category,
            metadata.EntityId,
            metadata.EntityType,
            metadata.UploadedBy,
            metadata.UploadedAt);

        return Result.Success(dto);
    }
}
