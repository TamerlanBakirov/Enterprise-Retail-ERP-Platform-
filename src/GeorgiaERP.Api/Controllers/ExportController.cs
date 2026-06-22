using GeorgiaERP.Application.Export;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeorgiaERP.Api.Controllers;

/// <summary>
/// Data export endpoints for downloading CSV reports of products, inventory,
/// sales transactions, customers, and audit logs.
/// </summary>
[Authorize]
[Tags("Export")]
public class ExportController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public ExportController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Exports the product catalog to CSV.
    /// </summary>
    [HttpGet("products")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportProducts(
        [FromQuery] string? search = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] bool? isActive = null)
    {
        var result = await _mediator.Send(new ExportProductsQuery(search, categoryId, isActive));
        return File(result.FileBytes, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Exports inventory stock levels to CSV.
    /// </summary>
    [HttpGet("inventory")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportInventory(
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] bool lowStockOnly = false)
    {
        var result = await _mediator.Send(new ExportInventoryQuery(warehouseId, lowStockOnly));
        return File(result.FileBytes, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Exports POS sales transactions to CSV for a given date range.
    /// </summary>
    [HttpGet("sales")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportSales(
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] Guid? storeId = null)
    {
        var result = await _mediator.Send(new ExportSalesQuery(from, to, storeId));
        return File(result.FileBytes, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Exports the customer list to CSV.
    /// </summary>
    [HttpGet("customers")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportCustomers(
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null)
    {
        var result = await _mediator.Send(new ExportCustomersQuery(search, isActive));
        return File(result.FileBytes, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Exports audit log entries to CSV. Restricted to administrators.
    /// </summary>
    [HttpGet("audit")]
    [Authorize(Roles = "super_admin,admin")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportAuditLog(
        [FromQuery] string? entityType = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null)
    {
        var result = await _mediator.Send(new ExportAuditLogQuery(entityType, userId, from, to));
        return File(result.FileBytes, result.ContentType, result.FileName);
    }
}
