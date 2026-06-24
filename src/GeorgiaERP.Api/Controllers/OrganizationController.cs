using GeorgiaERP.Application.Organization.Commands;
using GeorgiaERP.Application.Organization.DTOs;
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
public class OrganizationController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public OrganizationController(IMediator mediator) => _mediator = mediator;

    [HttpGet("company")]
    [EnableRateLimiting("read")]
    public async Task<IActionResult> GetCompany()
    {
        var result = await _mediator.Send(new GetCompanyQuery());
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpPost("company")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> CreateCompany([FromBody] CreateCompanyCommand command)
    {
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPut("company/{id:guid}")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> UpdateCompany(Guid id, [FromBody] UpdateCompanyCommand command)
    {
        if (id != command.Id) return BadRequest("ID mismatch.");
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpGet("stores")]
    [EnableRateLimiting("read")]
    public async Task<IActionResult> GetStores([FromQuery] bool? isActive = null)
    {
        var result = await _mediator.Send(new GetStoresQuery(isActive));
        return Ok(result);
    }

    [HttpGet("stores/{id:guid}")]
    [EnableRateLimiting("read")]
    public async Task<IActionResult> GetStoreById(Guid id)
    {
        var result = await _mediator.Send(new GetStoreByIdQuery(id));
        return ToActionResult(result);
    }

    [HttpPost("stores")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> CreateStore([FromBody] CreateStoreCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsFailure) return ToActionResult(result);
        return CreatedAtAction(nameof(GetStoreById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("stores/{id:guid}")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> UpdateStore(Guid id, [FromBody] UpdateStoreCommand command)
    {
        if (id != command.Id) return BadRequest("ID mismatch.");
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpGet("warehouses")]
    [EnableRateLimiting("read")]
    public async Task<IActionResult> GetWarehouses([FromQuery] bool? isActive = null)
    {
        var result = await _mediator.Send(new GetWarehousesQuery(isActive));
        return Ok(result);
    }
}
