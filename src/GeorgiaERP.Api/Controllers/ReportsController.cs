using GeorgiaERP.Application.Reporting.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GeorgiaERP.Api.Controllers;

/// <summary>
/// Reporting and analytics endpoints including sales reports, dashboard KPIs,
/// and business intelligence data.
/// </summary>
[Authorize]
[Tags("Reports")]
[EnableRateLimiting("read")]
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

    /// <summary>
    /// Returns aggregated KPI metrics for the dashboard. Cached for 2 minutes.
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardKpis()
    {
        var result = await _mediator.Send(new DashboardKpiQuery());
        return Ok(result);
    }
}
