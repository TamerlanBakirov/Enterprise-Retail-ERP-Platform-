using GeorgiaERP.Application.Reporting.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeorgiaERP.Api.Controllers;

[Authorize]
public class ReportsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("sales")]
    public async Task<IActionResult> GetSalesReport(
        [FromQuery] Guid? storeId,
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to)
    {
        var result = await _mediator.Send(new SalesReportQuery(storeId, from, to));
        return Ok(result);
    }

    [HttpGet("stock")]
    public async Task<IActionResult> GetStockReport([FromQuery] Guid? warehouseId = null)
    {
        var result = await _mediator.Send(new StockReportQuery(warehouseId));
        return Ok(result);
    }

    [HttpGet("vat")]
    public async Task<IActionResult> GetVatReport(
        [FromQuery] int? year = null,
        [FromQuery] int? month = null)
    {
        var now = DateTime.UtcNow;
        var result = await _mediator.Send(new VatReportQuery(year ?? now.Year, month ?? now.Month));
        return Ok(result);
    }
}
