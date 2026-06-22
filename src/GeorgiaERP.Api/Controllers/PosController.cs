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
        if (result.IsFailure)
            return ToActionResult(result);
        return Ok(result.Value);
    }

    [HttpPost("sessions/{sessionId:guid}/close")]
    public async Task<IActionResult> CloseSession(Guid sessionId, [FromBody] ClosePosSessionRequest request)
    {
        var command = new ClosePosSessionCommand(sessionId, request.ClosingBalance, request.Notes);
        var result = await _mediator.Send(command);
        if (result.IsFailure)
            return ToActionResult(result);
        return Ok(result.Value);
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
            return ToActionResult(result);
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
        return ToActionResult(result);
    }

    [HttpPost("transactions/{transactionId:guid}/void")]
    public async Task<IActionResult> VoidTransaction(Guid transactionId, [FromBody] VoidTransactionRequest request)
    {
        var command = new VoidPosTransactionCommand(transactionId, CurrentUserId, request.Reason);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}

public record ClosePosSessionRequest(decimal ClosingBalance, string? Notes = null);
public record VoidTransactionRequest(string Reason);
