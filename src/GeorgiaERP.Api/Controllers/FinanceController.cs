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

    [HttpGet("chart-of-accounts/{id:guid}")]
    public async Task<IActionResult> GetAccountById(Guid id)
    {
        var result = await _mediator.Send(new GetChartOfAccountByIdQuery(id));
        return result is null ? NotFound() : Ok(result);
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

    /// <summary>
    /// Reverses a posted journal entry by posting a mirror entry and marking the
    /// original as reversed.
    /// </summary>
    [HttpPost("journal-entries/{id:guid}/reverse")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> ReverseJournalEntry(Guid id, [FromBody] ReverseJournalEntryRequest? request = null)
    {
        var result = await _mediator.Send(new ReverseJournalEntryCommand(id, CurrentUserId, request?.Reason));
        if (result.IsFailure)
            return ToActionResult(result);
        return Created($"/api/v1/finance/journal-entries/{result.Value!.Id}", result.Value);
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

    /// <summary>
    /// Profit &amp; Loss statement: revenue, expenses, and net profit for the period.
    /// </summary>
    [HttpGet("income-statement")]
    public async Task<IActionResult> GetIncomeStatement(
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to)
    {
        if (from > to)
            return BadRequest(new { error = "'from' must be on or before 'to'." });

        var result = await _mediator.Send(new IncomeStatementQuery(from, to));
        return Ok(result);
    }

    /// <summary>
    /// Balance sheet as of a date: assets, liabilities, equity, and current-period earnings.
    /// </summary>
    [HttpGet("balance-sheet")]
    public async Task<IActionResult> GetBalanceSheet([FromQuery] DateTimeOffset? asOfDate = null)
    {
        var result = await _mediator.Send(new BalanceSheetQuery(asOfDate));
        return Ok(result);
    }

    /// <summary>
    /// General ledger detail for a single account: opening balance, posted
    /// transactions with running balance, and closing balance.
    /// </summary>
    [HttpGet("general-ledger/{accountId:guid}")]
    public async Task<IActionResult> GetGeneralLedger(
        Guid accountId,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null)
    {
        if (from.HasValue && to.HasValue && from > to)
            return BadRequest(new { error = "'from' must be on or before 'to'." });

        var result = await _mediator.Send(new GeneralLedgerQuery(accountId, from, to));
        return result is null ? NotFound() : Ok(result);
    }
}

public record ReverseJournalEntryRequest(string? Reason = null);
