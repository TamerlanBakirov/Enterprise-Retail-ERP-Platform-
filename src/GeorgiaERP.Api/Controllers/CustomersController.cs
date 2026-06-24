using GeorgiaERP.Application.CRM.Commands;
using GeorgiaERP.Application.CRM.DTOs;
using GeorgiaERP.Application.CRM.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GeorgiaERP.Api.Controllers;

[Authorize]
[Tags("CRM")]
[EnableRateLimiting("read")]
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

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCustomerById(Guid id)
    {
        var result = await _mediator.Send(new GetCustomerByIdQuery(id));
        return ToActionResult(result);
    }

    [HttpPost]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsFailure)
            return ToActionResult(result);
        return CreatedAtAction(nameof(GetCustomerById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> UpdateCustomer(Guid id, [FromBody] UpdateCustomerRequest request)
    {
        var result = await _mediator.Send(new UpdateCustomerCommand(
            id, request.FirstName, request.LastName,
            request.FirstNameKa, request.LastNameKa,
            request.CompanyName, request.Tin,
            request.Phone, request.Email,
            request.ConsentSms, request.ConsentEmail, request.IsActive));

        return ToActionResult(result);
    }

    [HttpPost("{customerId:guid}/loyalty/earn")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> EarnPoints(Guid customerId, [FromBody] EarnPointsRequest request)
    {
        var result = await _mediator.Send(new EarnLoyaltyPointsCommand(
            customerId, request.Points, request.ReferenceType, request.ReferenceId, request.Description));
        if (result.IsFailure)
            return ToActionResult(result);
        return Ok(new { balance = result.Value });
    }

    [HttpPost("{customerId:guid}/loyalty/redeem")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> RedeemPoints(Guid customerId, [FromBody] RedeemPointsRequest request)
    {
        var result = await _mediator.Send(new RedeemLoyaltyPointsCommand(customerId, request.Points, request.Description));
        if (result.IsFailure)
            return ToActionResult(result);
        return Ok(new { balance = result.Value });
    }

    [HttpGet("{customerId:guid}/loyalty/transactions")]
    public async Task<IActionResult> GetLoyaltyHistory(
        Guid customerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetLoyaltyHistoryQuery(customerId, page, pageSize));
        return ToActionResult(result);
    }
}

public record EarnPointsRequest(int Points, string? ReferenceType = null, Guid? ReferenceId = null, string? Description = null);
public record RedeemPointsRequest(int Points, string? Description = null);

