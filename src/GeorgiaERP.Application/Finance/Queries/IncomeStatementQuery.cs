using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Finance;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Finance.Queries;

/// <summary>
/// Profit &amp; Loss statement for a reporting period. Aggregates posted journal
/// activity on Revenue and Expense accounts between <paramref name="From"/> and
/// <paramref name="To"/> (inclusive) and returns net profit.
/// </summary>
public record IncomeStatementQuery(
    DateTimeOffset From,
    DateTimeOffset To) : IRequest<IncomeStatementResponse>;

public record IncomeStatementResponse(
    DateTimeOffset From,
    DateTimeOffset To,
    decimal TotalRevenue,
    decimal TotalExpenses,
    decimal NetProfit,
    IReadOnlyList<IncomeStatementLineDto> Revenue,
    IReadOnlyList<IncomeStatementLineDto> Expenses);

public record IncomeStatementLineDto(
    Guid AccountId,
    string AccountCode,
    string AccountName,
    string? AccountNameKa,
    decimal Amount);

public class IncomeStatementQueryHandler : IRequestHandler<IncomeStatementQuery, IncomeStatementResponse>
{
    private readonly IAppDbContext _db;
    public IncomeStatementQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IncomeStatementResponse> Handle(IncomeStatementQuery request, CancellationToken ct)
    {
        var from = request.From;
        var to = request.To;

        // Subquery (see TrialBalance): IN (SELECT ...) instead of a materialized id list.
        var postedEntryIds = _db.JournalEntries
            .AsNoTracking()
            .Where(j => j.Status == JournalEntryStatus.Posted && j.EntryDate >= from && j.EntryDate <= to)
            .Select(j => j.Id);

        var aggregates = await _db.JournalEntryLines
            .AsNoTracking()
            .Where(l => postedEntryIds.Contains(l.JournalEntryId))
            .GroupBy(l => l.AccountId)
            .Select(g => new
            {
                AccountId = g.Key,
                DebitTotal = g.Sum(l => l.DebitAmount),
                CreditTotal = g.Sum(l => l.CreditAmount)
            })
            .ToListAsync(ct);

        var accounts = await _db.ChartOfAccounts
            .AsNoTracking()
            .Where(a => !a.IsHeader && a.IsActive &&
                        (a.AccountType == AccountType.Revenue || a.AccountType == AccountType.Expense))
            .ToDictionaryAsync(a => a.Id, ct);

        var revenue = new List<IncomeStatementLineDto>();
        var expenses = new List<IncomeStatementLineDto>();

        foreach (var agg in aggregates.OrderBy(a =>
            accounts.TryGetValue(a.AccountId, out var acc) ? acc.AccountCode : ""))
        {
            if (!accounts.TryGetValue(agg.AccountId, out var account))
                continue;

            var amount = account.BalanceType == BalanceType.Debit
                ? agg.DebitTotal - agg.CreditTotal
                : agg.CreditTotal - agg.DebitTotal;

            var line = new IncomeStatementLineDto(
                account.Id, account.AccountCode, account.Name, account.NameKa, amount);

            if (account.AccountType == AccountType.Revenue)
                revenue.Add(line);
            else
                expenses.Add(line);
        }

        var totalRevenue = revenue.Sum(l => l.Amount);
        var totalExpenses = expenses.Sum(l => l.Amount);

        return new IncomeStatementResponse(
            from, to,
            totalRevenue,
            totalExpenses,
            totalRevenue - totalExpenses,
            revenue,
            expenses);
    }
}
