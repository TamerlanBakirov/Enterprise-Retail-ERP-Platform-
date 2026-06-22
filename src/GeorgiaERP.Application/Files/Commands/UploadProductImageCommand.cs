using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Files.DTOs;
using GeorgiaERP.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Files.Commands;

public record UploadProductImageCommand(
    Guid ProductId,
    Stream FileStream,
    string FileName,
    string ContentType,
    long SizeBytes,
    Guid UploadedBy) : IRequest<Result<FileMetadataDto>>;

public class UploadProductImageCommandHandler : IRequestHandler<UploadProductImageCommand, Result<FileMetadataDto>>
{
    private readonly IAppDbContext _db;
    private readonly IFileStorageService _fileStorage;

    public UploadProductImageCommandHandler(IAppDbContext db, IFileStorageService fileStorage)
    {
        _db = db;
        _fileStorage = fileStorage;
    }

    public async Task<Result<FileMetadataDto>> Handle(UploadProductImageCommand request, CancellationToken cancellationToken)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);
        if (product is null)
            return Result.NotFound<FileMetadataDto>("Product", request.ProductId);

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
            "product-image",
            request.ProductId,
            "Product");

        _db.FileMetadata.Add(metadata);

        // Update product image URL
        var imageUrl = $"/api/v1/files/{metadata.Id}";
        product.SetImageUrl(imageUrl);

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
