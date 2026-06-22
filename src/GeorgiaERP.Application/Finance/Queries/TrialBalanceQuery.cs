using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Finance;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Finance.Queries;

public record TrialBalanceQuery(
    DateTimeOffset? AsOfDate = null) : IRequest<TrialBalanceResponse>;

public record TrialBalanceResponse(
    DateTimeOffset AsOfDate,
    decimal TotalDebit,
    decimal TotalCredit,
    bool IsBalanced,
    IReadOnlyList<TrialBalanceLineDto> Lines);

public record TrialBalanceLineDto(
    Guid AccountId,
    string AccountCode,
    string AccountName,
    string? AccountNameKa,
    string AccountType,
    string BalanceType,
    decimal DebitTotal,
    decimal CreditTotal,
    decimal Balance);

public class TrialBalanceQueryHandler : IRequestHandler<TrialBalanceQuery, TrialBalanceResponse>
{
    private readonly IAppDbContext _db;
    public TrialBalanceQueryHandler(IAppDbContext db) => _db = db;

    public async Task<TrialBalanceResponse> Handle(TrialBalanceQuery request, CancellationToken ct)
    {
        var asOf = request.AsOfDate ?? DateTimeOffset.UtcNow;

        var postedEntryIds = await _db.JournalEntries
            .AsNoTracking()
            .Where(j => j.Status == JournalEntryStatus.Posted && j.EntryDate <= asOf)
            .Select(j => j.Id)
            .ToListAsync(ct);

        var lineAggregates = await _db.JournalEntryLines
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

        var lines = new List<TrialBalanceLineDto>();

        foreach (var agg in lineAggregates.OrderBy(a =>
            accounts.TryGetValue(a.AccountId, out var acc) ? acc.AccountCode : ""))
        {
            if (!accounts.TryGetValue(agg.AccountId, out var account))
                continue;

            var balance = account.BalanceType == BalanceType.Debit
                ? agg.DebitTotal - agg.CreditTotal
                : agg.CreditTotal - agg.DebitTotal;

            lines.Add(new TrialBalanceLineDto(
                agg.AccountId,
                account.AccountCode,
                account.Name,
                account.NameKa,
                account.AccountType.ToString(),
                account.BalanceType.ToString(),
                agg.DebitTotal,
                agg.CreditTotal,
                balance));
        }

        var totalDebit = lines.Sum(l => l.DebitTotal);
        var totalCredit = lines.Sum(l => l.CreditTotal);

        return new TrialBalanceResponse(
            asOf, totalDebit, totalCredit,
            Math.Abs(totalDebit - totalCredit) < 0.01m,
            lines);
    }
}
