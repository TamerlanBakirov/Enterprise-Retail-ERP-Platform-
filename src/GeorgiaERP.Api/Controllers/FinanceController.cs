using GeorgiaERP.Application.Finance.Commands;
using GeorgiaERP.Application.Finance.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GeorgiaERP.Api.Controllers;

/// <summary>
/// Financial management including chart of accounts, journal entries,
/// and bank account operations for Georgian accounting standards.
/// </summary>
[Authorize]
[Tags("Finance")]
[EnableRateLimiting("read")]
public class FinanceController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public FinanceController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("chart-of-accounts")]
    public async Task<IActionResult> GetChartOfAccounts([FromQuery] bool? isActive = null)
    {
        var result = await _mediator.Send(new GetChartOfAccountsQuery(isActive));
        return Ok(result);
    }

    [HttpPost("chart-of-accounts")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsFailure)
            return ToActionResult(result);
        return Created($"/api/v1/finance/chart-of-accounts/{result.Value}", new { id = result.Value });
    }

    [HttpGet("journal-entries")]
    public async Task<IActionResult> GetJournalEntries(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetJournalEntriesQuery(status, page, pageSize));
        return Ok(result);
    }

    [HttpPost("journal-entries")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> CreateJournalEntry([FromBody] CreateJournalEntryCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsFailure)
            return ToActionResult(result);
        return Created($"/api/v1/finance/journal-entries/{result.Value!.Id}", result.Value);
    }

    [HttpPost("journal-entries/{id:guid}/post")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> PostJournalEntry(Guid id)
    {
        var result = await _mediator.Send(new PostJournalEntryCommand(id, CurrentUserId));
        return ToActionResult(result);
    }

    [HttpGet("bank-accounts")]
    public async Task<IActionResult> GetBankAccounts()
    {
        var result = await _mediator.Send(new GetBankAccountsQuery());
        return Ok(result);
    }

    [HttpPost("bank-accounts")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> CreateBankAccount([FromBody] CreateBankAccountCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsFailure)
            return ToActionResult(result);
        return Created($"/api/v1/finance/bank-accounts/{result.Value}", new { id = result.Value });
    }

    [HttpGet("trial-balance")]
    public async Task<IActionResult> GetTrialBalance([FromQuery] DateTimeOffset? asOfDate = null)
    {
        var result = await _mediator.Send(new TrialBalanceQuery(asOfDate));
        return Ok(result);
    }
}
