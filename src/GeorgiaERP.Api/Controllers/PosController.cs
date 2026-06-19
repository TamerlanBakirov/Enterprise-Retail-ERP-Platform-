using GeorgiaERP.Application.POS.Commands;
using GeorgiaERP.Application.POS.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeorgiaERP.Api.Controllers;

[Authorize]
public class PosController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public PosController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("sessions")]
    public async Task<IActionResult> OpenSession([FromBody] OpenPosSessionCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("sessions/{sessionId:guid}/close")]
    public async Task<IActionResult> CloseSession(Guid sessionId, [FromBody] ClosePosSessionRequest request)
    {
        var command = new ClosePosSessionCommand(sessionId, request.ClosingBalance, request.Notes);
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions(
        [FromQuery] Guid? terminalId = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetPosSessionsQuery(terminalId, status, page, pageSize));
        return Ok(result);
    }

    [HttpPost("transactions")]
    public async Task<IActionResult> CreateTransaction([FromBody] CreatePosTransactionCommand command)
    {
        var result = await _mediator.Send(command);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Created($"/api/v1/pos/transactions/{result.Value!.TransactionId}", result.Value);
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] Guid? sessionId = null,
        [FromQuery] Guid? storeId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(
            new GetPosTransactionsQuery(sessionId, storeId, status, from, to, page, pageSize));
        return Ok(result);
    }

    [HttpGet("transactions/{transactionId:guid}")]
    public async Task<IActionResult> GetTransaction(Guid transactionId)
    {
        var result = await _mediator.Send(new GetPosTransactionDetailQuery(transactionId));
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpPost("transactions/{transactionId:guid}/void")]
    public async Task<IActionResult> VoidTransaction(Guid transactionId, [FromBody] VoidTransactionRequest request)
    {
        var command = new VoidPosTransactionCommand(transactionId, CurrentUserId, request.Reason);
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }
}

public record ClosePosSessionRequest(decimal ClosingBalance, string? Notes = null);
public record VoidTransactionRequest(string Reason);
