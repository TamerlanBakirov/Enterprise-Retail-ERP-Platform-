using GeorgiaERP.Application.Analytics.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeorgiaERP.Api.Controllers;

[Authorize]
public class AnalyticsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public AnalyticsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var result = await _mediator.Send(new GetDashboardSummaryQuery());
        return Ok(result);
    }

    [HttpGet("revenue-trend")]
    public async Task<IActionResult> GetRevenueTrend([FromQuery] int days = 30)
    {
        var result = await _mediator.Send(new GetRevenueTrendQuery(days));
        return Ok(result);
    }

    [HttpGet("sales-by-category")]
    public async Task<IActionResult> GetSalesByCategory()
    {
        var result = await _mediator.Send(new GetSalesByCategoryQuery());
        return Ok(result);
    }

    [HttpGet("stock-summary")]
    public async Task<IActionResult> GetStockSummary()
    {
        var result = await _mediator.Send(new GetStockSummaryQuery());
        return Ok(result);
    }
}
