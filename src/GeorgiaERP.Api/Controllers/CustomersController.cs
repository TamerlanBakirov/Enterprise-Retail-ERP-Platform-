using GeorgiaERP.Application.CRM.Commands;
using GeorgiaERP.Application.CRM.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeorgiaERP.Api.Controllers;

/// <summary>
/// Customer relationship management (CRM) including customer profiles and loyalty programs.
/// </summary>
[Authorize]
[Tags("CRM")]
public class CustomersController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public CustomersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetCustomersQuery(search, isActive, page, pageSize));
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsFailure)
            return ToActionResult(result);
        return Created($"/api/v1/customers/{result.Value!.Id}", result.Value);
    }

    [HttpPost("{customerId:guid}/loyalty/earn")]
    public async Task<IActionResult> EarnPoints(Guid customerId, [FromBody] EarnPointsRequest request)
    {
        var result = await _mediator.Send(new EarnLoyaltyPointsCommand(
            customerId, request.Points, request.ReferenceType, request.ReferenceId, request.Description));
        if (result.IsFailure)
            return ToActionResult(result);
        return Ok(new { balance = result.Value });
    }

    [HttpPost("{customerId:guid}/loyalty/redeem")]
    public async Task<IActionResult> RedeemPoints(Guid customerId, [FromBody] RedeemPointsRequest request)
    {
        var result = await _mediator.Send(new RedeemLoyaltyPointsCommand(customerId, request.Points, request.Description));
        if (result.IsFailure)
            return ToActionResult(result);
        return Ok(new { balance = result.Value });
    }
}

public record EarnPointsRequest(int Points, string? ReferenceType = null, Guid? ReferenceId = null, string? Description = null);
public record RedeemPointsRequest(int Points, string? Description = null);

