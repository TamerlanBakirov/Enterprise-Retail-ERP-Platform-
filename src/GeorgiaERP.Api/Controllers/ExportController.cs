using GeorgiaERP.Application.Export;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GeorgiaERP.Api.Controllers;

/// <summary>
/// Data export endpoints for downloading CSV or Excel reports of products, inventory,
/// sales transactions, customers, and audit logs.
/// Use ?format=xlsx for Excel or ?format=csv (default) for CSV.
/// </summary>
[Authorize]
[Tags("Export")]
[EnableRateLimiting("export")]
public class ExportController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public ExportController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Exports the product catalog to CSV or Excel.
    /// </summary>
    [HttpGet("products")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportProducts(
        [FromQuery] string? search = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string format = "csv")
    {
        var result = await _mediator.Send(new ExportProductsQuery(search, categoryId, isActive, ParseFormat(format)));
        return File(result.FileBytes, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Exports inventory stock levels to CSV or Excel.
    /// </summary>
    [HttpGet("inventory")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportInventory(
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] bool lowStockOnly = false,
        [FromQuery] string format = "csv")
    {
        var result = await _mediator.Send(new ExportInventoryQuery(warehouseId, lowStockOnly, ParseFormat(format)));
        return File(result.FileBytes, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Exports POS sales transactions to CSV or Excel for a given date range.
    /// </summary>
    [HttpGet("sales")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportSales(
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] Guid? storeId = null,
        [FromQuery] string format = "csv")
    {
        var result = await _mediator.Send(new ExportSalesQuery(from, to, storeId, ParseFormat(format)));
        return File(result.FileBytes, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Exports the customer list to CSV or Excel.
    /// </summary>
    [HttpGet("customers")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportCustomers(
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string format = "csv")
    {
        var result = await _mediator.Send(new ExportCustomersQuery(search, isActive, ParseFormat(format)));
        return File(result.FileBytes, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Exports audit log entries to CSV or Excel. Restricted to administrators.
    /// </summary>
    [HttpGet("audit")]
    [Authorize(Roles = "super_admin,admin")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportAuditLog(
        [FromQuery] string? entityType = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] string format = "csv")
    {
        var result = await _mediator.Send(new ExportAuditLogQuery(entityType, userId, from, to, ParseFormat(format)));
        return File(result.FileBytes, result.ContentType, result.FileName);
    }

    private static ExportFormat ParseFormat(string format)
    {
        return format.Equals("xlsx", StringComparison.OrdinalIgnoreCase)
            || format.Equals("excel", StringComparison.OrdinalIgnoreCase)
            ? ExportFormat.Excel
            : ExportFormat.Csv;
    }
}
