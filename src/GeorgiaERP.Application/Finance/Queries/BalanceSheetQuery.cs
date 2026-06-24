using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Finance;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Finance.Queries;

/// <summary>
/// Balance sheet as of a given date. Reports Asset, Liability and Equity balances
/// from posted journal activity up to <paramref name="AsOfDate"/>. Current-period
/// earnings (Revenue - Expenses) are folded into equity so the statement balances:
/// Assets = Liabilities + Equity + CurrentEarnings.
/// </summary>
public record BalanceSheetQuery(
    DateTimeOffset? AsOfDate = null) : IRequest<BalanceSheetResponse>;

public record BalanceSheetResponse(
    DateTimeOffset AsOfDate,
    decimal TotalAssets,
    decimal TotalLiabilities,
    decimal TotalEquity,
    decimal CurrentEarnings,
    bool IsBalanced,
    IReadOnlyList<BalanceSheetLineDto> Assets,
    IReadOnlyList<BalanceSheetLineDto> Liabilities,
    IReadOnlyList<BalanceSheetLineDto> Equity);

public record BalanceSheetLineDto(
    Guid AccountId,
    string AccountCode,
    string AccountName,
    string? AccountNameKa,
    decimal Balance);

public class BalanceSheetQueryHandler : IRequestHandler<BalanceSheetQuery, BalanceSheetResponse>
{
    private readonly IAppDbContext _db;
    public BalanceSheetQueryHandler(IAppDbContext db) => _db = db;

    public async Task<BalanceSheetResponse> Handle(BalanceSheetQuery request, CancellationToken ct)
    {
        var asOf = request.AsOfDate ?? DateTimeOffset.UtcNow;

        var postedEntryIds = await _db.JournalEntries
            .AsNoTracking()
            .Where(j => j.Status == JournalEntryStatus.Posted && j.EntryDate <= asOf)
            .Select(j => j.Id)
            .ToListAsync(ct);

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
            .Where(a => !a.IsHeader && a.IsActive)
            .ToDictionaryAsync(a => a.Id, ct);

        var assets = new List<BalanceSheetLineDto>();
        var liabilities = new List<BalanceSheetLineDto>();
        var equity = new List<BalanceSheetLineDto>();
        decimal currentEarnings = 0m;

        foreach (var agg in aggregates.OrderBy(a =>
            accounts.TryGetValue(a.AccountId, out var acc) ? acc.AccountCode : ""))
        {
            if (!accounts.TryGetValue(agg.AccountId, out var account))
                continue;

            var balance = account.BalanceType == BalanceType.Debit
                ? agg.DebitTotal - agg.CreditTotal
                : agg.CreditTotal - agg.DebitTotal;

            var line = new BalanceSheetLineDto(
                account.Id, account.AccountCode, account.Name, account.NameKa, balance);

            switch (account.AccountType)
            {
                case AccountType.Asset:
                    assets.Add(line);
                    break;
                case AccountType.Liability:
                    liabilities.Add(line);
                    break;
                case AccountType.Equity:
                    equity.Add(line);
                    break;
                case AccountType.Revenue:
                    currentEarnings += balance;
                    break;
                case AccountType.Expense:
                    currentEarnings -= balance;
                    break;
            }
        }

        var totalAssets = assets.Sum(l => l.Balance);
        var totalLiabilities = liabilities.Sum(l => l.Balance);
        var totalEquity = equity.Sum(l => l.Balance);

        return new BalanceSheetResponse(
            asOf,
            totalAssets,
            totalLiabilities,
            totalEquity,
            currentEarnings,
            Math.Abs(totalAssets - (totalLiabilities + totalEquity + currentEarnings)) < 0.01m,
            assets,
            liabilities,
            equity);
    }
}
