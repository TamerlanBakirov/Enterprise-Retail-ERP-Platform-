using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Finance.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Api.Controllers;

[Authorize]
public class FinanceController : ApiControllerBase
{
    private readonly IAppDbContext _dbContext;
    private readonly IMediator _mediator;

    public FinanceController(IAppDbContext dbContext, IMediator mediator)
    {
        _dbContext = dbContext;
        _mediator = mediator;
    }

    [HttpGet("chart-of-accounts")]
    public async Task<IActionResult> GetChartOfAccounts([FromQuery] bool? isActive = null)
    {
        var query = _dbContext.ChartOfAccounts.AsQueryable();

        if (isActive.HasValue)
            query = query.Where(a => a.IsActive == isActive.Value);

        var accounts = await query
            .OrderBy(a => a.AccountCode)
            .Select(a => new
            {
                a.Id, a.AccountCode, a.Name, a.NameKa,
                AccountType = a.AccountType.ToString(),
                a.ParentId, a.IsHeader, a.IsSystem,
                BalanceType = a.BalanceType.ToString(), a.IsActive
            })
            .ToListAsync();

        return Ok(accounts);
    }

    [HttpPost("chart-of-accounts")]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Created($"/api/v1/finance/chart-of-accounts/{result.Value}", new { id = result.Value }) : BadRequest(new { error = result.Error });
    }

    [HttpGet("journal-entries")]
    public async Task<IActionResult> GetJournalEntries(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _dbContext.JournalEntries.AsQueryable();

        if (!string.IsNullOrEmpty(status) &&
            Enum.TryParse<Domain.Finance.JournalEntryStatus>(status, true, out var entryStatus))
            query = query.Where(j => j.Status == entryStatus);

        var totalCount = await query.CountAsync();

        var entries = await query
            .OrderByDescending(j => j.EntryDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new
            {
                j.Id, j.EntryNumber, j.EntryDate, j.Description,
                Status = j.Status.ToString(), j.TotalDebit, j.TotalCredit, j.PostedAt, j.CreatedAt
            })
            .ToListAsync();

        return Ok(new { Items = entries, TotalCount = totalCount, Page = page, PageSize = pageSize });
    }

    [HttpPost("journal-entries")]
    public async Task<IActionResult> CreateJournalEntry([FromBody] CreateJournalEntryCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Created($"/api/v1/finance/journal-entries/{result.Value!.Id}", result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("journal-entries/{id:guid}/post")]
    public async Task<IActionResult> PostJournalEntry(Guid id)
    {
        var result = await _mediator.Send(new PostJournalEntryCommand(id, CurrentUserId));
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }

    [HttpGet("bank-accounts")]
    public async Task<IActionResult> GetBankAccounts()
    {
        var accounts = await _dbContext.BankAccounts
            .OrderBy(a => a.AccountName)
            .Select(a => new
            {
                a.Id, a.AccountName, a.BankName, a.AccountNumber,
                a.Iban, a.Currency, a.CurrentBalance, a.IsActive
            })
            .ToListAsync();

        return Ok(accounts);
    }

    [HttpPost("bank-accounts")]
    public async Task<IActionResult> CreateBankAccount([FromBody] CreateBankAccountCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Created($"/api/v1/finance/bank-accounts/{result.Value}", new { id = result.Value }) : BadRequest(new { error = result.Error });
    }
}
