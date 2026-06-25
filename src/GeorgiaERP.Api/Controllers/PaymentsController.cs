using GeorgiaERP.Application.Payments.Commands;
using GeorgiaERP.Application.Payments.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeorgiaERP.Api.Controllers;

[Authorize]
public class PaymentsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public PaymentsController(IMediator mediator) => _mediator = mediator;

    [HttpPost("initiate")]
    public async Task<IActionResult> Initiate([FromBody] InitiatePaymentCommand command)
    {
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback([FromBody] ProcessPaymentCallbackCommand command)
    {
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/refund")]
    public async Task<IActionResult> Refund(Guid id, [FromBody] RefundPaymentRequest request)
    {
        var result = await _mediator.Send(new RefundPaymentCommand(id, request.Amount));
        return ToActionResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetPaymentTransactionsQuery(Page: page, PageSize: pageSize));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetPaymentTransactionByIdQuery(id));
        return result is not null ? Ok(result) : NotFound();
    }

    public record RefundPaymentRequest(decimal? Amount);
}
