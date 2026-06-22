using GeorgiaERP.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Files.Queries;

public record GetFileQuery(Guid FileId) : IRequest<Result<FileDownloadResult>>;

public record FileDownloadResult(Stream Content, string ContentType, string FileName);

public class GetFileQueryHandler : IRequestHandler<GetFileQuery, Result<FileDownloadResult>>
{
    private readonly IAppDbContext _db;
    private readonly IFileStorageService _fileStorage;

    public GetFileQueryHandler(IAppDbContext db, IFileStorageService fileStorage)
    {
        _db = db;
        _fileStorage = fileStorage;
    }

    public async Task<Result<FileDownloadResult>> Handle(GetFileQuery request, CancellationToken cancellationToken)
    {
        var metadata = await _db.FileMetadata
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == request.FileId, cancellationToken);

        if (metadata is null)
            return Result.NotFound<FileDownloadResult>("File", request.FileId);

        var result = await _fileStorage.GetAsync(metadata.StoredFileName, cancellationToken);
        if (result is null)
            return Result.Failure<FileDownloadResult>("File content not found on storage.", "NOT_FOUND");

        return Result.Success(new FileDownloadResult(result.Content, metadata.ContentType, metadata.FileName));
    }
}
