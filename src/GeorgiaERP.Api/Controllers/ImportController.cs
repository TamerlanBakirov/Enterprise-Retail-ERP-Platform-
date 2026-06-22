using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Import;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GeorgiaERP.Api.Controllers;

/// <summary>
/// Data import endpoints for uploading CSV or Excel files of products and inventory.
/// Files are validated row-by-row with detailed error reports for invalid data.
/// </summary>
[Authorize(Roles = "super_admin,admin")]
[Tags("Import")]
[EnableRateLimiting("export")] // Reuse export rate limit (heavy operations)
public class ImportController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private static readonly string[] AllowedContentTypes =
    [
        "text/csv",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
    ];

    public ImportController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Imports products from a CSV or Excel file.
    /// Required columns: SKU, Name, CategoryId, UnitOfMeasure.
    /// Optional: NameKa, Description, VatApplicable, WeightKg, MinStockLevel,
    ///           MaxStockLevel, ReorderPoint, ReorderQty.
    /// </summary>
    [HttpPost("products")]
    [ProducesResponseType(typeof(ImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<IActionResult> ImportProducts(IFormFile file)
    {
        var validationError = ValidateFile(file);
        if (validationError is not null)
            return BadRequest(new { error = validationError });

        using var stream = file.OpenReadStream();
        var result = await _mediator.Send(new ImportProductsCommand(stream, file.ContentType));

        if (result.ErrorCount > 0 && result.SuccessCount == 0)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Imports inventory stock adjustments from a CSV or Excel file.
    /// Required columns: SKU, WarehouseId, Quantity.
    /// Optional: CostPrice.
    /// </summary>
    [HttpPost("inventory")]
    [ProducesResponseType(typeof(ImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<IActionResult> ImportInventory(IFormFile file)
    {
        var validationError = ValidateFile(file);
        if (validationError is not null)
            return BadRequest(new { error = validationError });

        using var stream = file.OpenReadStream();
        var result = await _mediator.Send(new ImportInventoryCommand(stream, file.ContentType));

        if (result.ErrorCount > 0 && result.SuccessCount == 0)
            return BadRequest(result);

        return Ok(result);
    }

    private static string? ValidateFile(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return "File is required and must not be empty.";

        if (file.Length > 10 * 1024 * 1024)
            return "File size must not exceed 10 MB.";

        if (!AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            var fileName = file.FileName;
            // Fallback: check file extension
            if (!fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
                && !fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return "File must be a CSV (.csv) or Excel (.xlsx) file.";
            }
        }

        return null;
    }
}
