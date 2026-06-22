using GeorgiaERP.Application.Organization.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GeorgiaERP.Api.Controllers;

/// <summary>
/// Organization structure management including companies, stores, and warehouses.
/// </summary>
[Authorize]
[Tags("Organization")]
[EnableRateLimiting("read")]
public class OrganizationController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public OrganizationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("stores")]
    public async Task<IActionResult> GetStores([FromQuery] bool? isActive = null)
    {
        var result = await _mediator.Send(new GetStoresQuery(isActive));
        return Ok(result);
    }

    [HttpGet("warehouses")]
    public async Task<IActionResult> GetWarehouses([FromQuery] bool? isActive = null)
    {
        var result = await _mediator.Send(new GetWarehousesQuery(isActive));
        return Ok(result);
    }
}
