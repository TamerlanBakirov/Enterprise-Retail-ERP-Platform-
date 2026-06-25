using GeorgiaERP.Application.Files.Commands;
using GeorgiaERP.Application.Files.Queries;
using GeorgiaERP.Infrastructure.FileStorage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace GeorgiaERP.Api.Controllers;

/// <summary>
/// File upload and download endpoints for product images and document attachments.
/// Supports configurable file type and size validation.
/// </summary>
[Authorize]
[Tags("Files")]
public class FilesController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly FileStorageOptions _fileOptions;

    public FilesController(IMediator mediator, IOptions<FileStorageOptions> fileOptions)
    {
        _mediator = mediator;
        _fileOptions = fileOptions.Value;
    }

    /// <summary>
    /// Uploads a product image and links it to the specified product.
    /// </summary>
    /// <param name="id">The product ID to attach the image to.</param>
    /// <param name="file">The image file (JPEG, PNG, GIF, WebP, BMP).</param>
    [HttpPost("/api/v1/products/{id:guid}/image")]
    [EnableRateLimiting("write")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<IActionResult> UploadProductImage(Guid id, IFormFile file)
    {
        var validation = ValidateFile(file, imageOnly: true);
        if (validation is not null)
            return validation;

        await using var stream = file.OpenReadStream();
        var command = new UploadProductImageCommand(
            id, stream, file.FileName, file.ContentType, file.Length, CurrentUserId);

        var result = await _mediator.Send(command);
        if (result.IsFailure)
            return ToActionResult(result);

        return CreatedAtAction(nameof(GetFile), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Uploads a document attachment (PDF, Word, Excel, CSV, or text).
    /// </summary>
    /// <param name="file">The document file.</param>
    /// <param name="category">Optional category tag for organization.</param>
    /// <param name="entityId">Optional entity ID to link the document to.</param>
    /// <param name="entityType">Optional entity type name (e.g., "PurchaseOrder").</param>
    [HttpPost("/api/v1/documents/upload")]
    [EnableRateLimiting("write")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<IActionResult> UploadDocument(
        IFormFile file,
        [FromQuery] string? category = null,
        [FromQuery] Guid? entityId = null,
        [FromQuery] string? entityType = null)
    {
        var validation = ValidateFile(file, imageOnly: false);
        if (validation is not null)
            return validation;

        await using var stream = file.OpenReadStream();
        var command = new UploadFileCommand(
            stream, file.FileName, file.ContentType, file.Length, CurrentUserId,
            category, entityId, entityType);

        var result = await _mediator.Send(command);
        if (result.IsFailure)
            return ToActionResult(result);

        return CreatedAtAction(nameof(GetFile), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Downloads a file by its unique identifier.
    /// </summary>
    /// <param name="id">The file metadata ID.</param>
    [HttpGet("{id:guid}")]
    [EnableRateLimiting("read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFile(Guid id)
    {
        var result = await _mediator.Send(new GetFileQuery(id));
        if (result.IsFailure)
            return ToActionResult(result);

        var file = result.Value!;
        // Defend against content sniffing / stored-XSS from user-uploaded files:
        // never let the browser re-interpret the content type, and force a
        // download rather than inline rendering (e.g. a .svg/.html with script).
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(file.Content, file.ContentType, file.FileName, enableRangeProcessing: false);
    }

    private IActionResult? ValidateFile(IFormFile? file, bool imageOnly)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file provided or file is empty.", errorCode = "VALIDATION_ERROR" });

        if (file.Length > _fileOptions.MaxFileSizeBytes)
            return BadRequest(new { error = $"File size exceeds the maximum allowed size of {_fileOptions.MaxFileSizeBytes / (1024 * 1024)} MB.", errorCode = "VALIDATION_ERROR" });

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (imageOnly)
        {
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };
            if (!imageExtensions.Contains(extension))
                return BadRequest(new { error = $"Invalid image file type. Allowed: {string.Join(", ", imageExtensions)}", errorCode = "VALIDATION_ERROR" });

            // Verify the actual bytes are a real image, not a script-bearing file
            // (e.g. .svg/.html) renamed with an image extension and spoofed type.
            if (!HasValidImageSignature(file))
                return BadRequest(new { error = "File content does not match a supported image format.", errorCode = "VALIDATION_ERROR" });
        }
        else if (_fileOptions.AllowedExtensions.Length > 0 && !_fileOptions.AllowedExtensions.Contains(extension))
        {
            return BadRequest(new { error = $"File type '{extension}' is not allowed.", errorCode = "VALIDATION_ERROR" });
        }

        if (_fileOptions.AllowedContentTypes.Length > 0 && !_fileOptions.AllowedContentTypes.Contains(file.ContentType))
        {
            return BadRequest(new { error = $"Content type '{file.ContentType}' is not allowed.", errorCode = "VALIDATION_ERROR" });
        }

        return null;
    }

    /// <summary>Checks the leading bytes against common raster image signatures.</summary>
    private static bool HasValidImageSignature(IFormFile file)
    {
        Span<byte> h = stackalloc byte[12];
        using var s = file.OpenReadStream();
        var read = s.Read(h);
        if (read < 4) return false;

        // JPEG FF D8 FF
        if (h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF) return true;
        // PNG 89 50 4E 47
        if (h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47) return true;
        // GIF 47 49 46 38
        if (h[0] == 0x47 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x38) return true;
        // BMP 42 4D
        if (h[0] == 0x42 && h[1] == 0x4D) return true;
        // WEBP "RIFF"...."WEBP"
        if (read >= 12 && h[0] == 0x52 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x46
            && h[8] == 0x57 && h[9] == 0x45 && h[10] == 0x42 && h[11] == 0x50) return true;

        return false;
    }
}
