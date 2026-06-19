using GeorgiaERP.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Api.Controllers;

[Authorize]
public class FinanceController : ApiControllerBase
{
    private readonly IAppDbContext _dbContext;

    public FinanceController(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
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
                a.Id,
                a.AccountCode,
                a.Name,
                a.NameKa,
                AccountType = a.AccountType.ToString(),
                a.ParentId,
                a.IsHeader,
                a.IsSystem,
                BalanceType = a.BalanceType.ToString(),
                a.IsActive
            })
            .ToListAsync();

        return Ok(accounts);
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
                j.Id,
                j.EntryNumber,
                j.EntryDate,
                j.Description,
                Status = j.Status.ToString(),
                j.TotalDebit,
                j.TotalCredit,
                j.PostedAt,
                j.CreatedAt
            })
            .ToListAsync();

        return Ok(new { Items = entries, TotalCount = totalCount, Page = page, PageSize = pageSize });
    }

    [HttpGet("bank-accounts")]
    public async Task<IActionResult> GetBankAccounts()
    {
        var accounts = await _dbContext.BankAccounts
            .OrderBy(a => a.AccountName)
            .Select(a => new
            {
                a.Id,
                a.AccountName,
                a.BankName,
                a.AccountNumber,
                a.Iban,
                a.Currency,
                a.CurrentBalance,
                a.IsActive
            })
            .ToListAsync();

        return Ok(accounts);
    }
}
