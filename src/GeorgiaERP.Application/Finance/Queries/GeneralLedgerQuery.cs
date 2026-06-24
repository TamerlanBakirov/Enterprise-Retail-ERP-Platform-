using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Finance;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Finance.Queries;

/// <summary>
/// General ledger detail for a single account: opening balance, every posted
/// transaction within the period with a running balance, and closing balance.
/// Returns null when the account does not exist.
/// </summary>
public record GeneralLedgerQuery(
    Guid AccountId,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null) : IRequest<GeneralLedgerResponse?>;

public record GeneralLedgerResponse(
    Guid AccountId,
    string AccountCode,
    string AccountName,
    string? AccountNameKa,
    string AccountType,
    string BalanceType,
    DateTimeOffset From,
    DateTimeOffset To,
    decimal OpeningBalance,
    decimal ClosingBalance,
    decimal TotalDebit,
    decimal TotalCredit,
    IReadOnlyList<GeneralLedgerLineDto> Lines);

public record GeneralLedgerLineDto(
    DateTimeOffset EntryDate,
    string EntryNumber,
    string? Description,
    decimal Debit,
    decimal Credit,
    decimal RunningBalance);

public class GeneralLedgerQueryHandler : IRequestHandler<GeneralLedgerQuery, GeneralLedgerResponse?>
{
    private readonly IAppDbContext _db;
    public GeneralLedgerQueryHandler(IAppDbContext db) => _db = db;

    public async Task<GeneralLedgerResponse?> Handle(GeneralLedgerQuery request, CancellationToken ct)
    {
        var account = await _db.ChartOfAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AccountId, ct);

        if (account is null)
            return null;

        var from = request.From ?? DateTimeOffset.MinValue;
        var to = request.To ?? DateTimeOffset.UtcNow;
        var isDebitAccount = account.BalanceType == BalanceType.Debit;

        // Posted lines for this account joined with their entry date.
        var ledgerLines = _db.JournalEntryLines
            .AsNoTracking()
            .Where(l => l.AccountId == request.AccountId)
            .Join(_db.JournalEntries.Where(j => j.Status == JournalEntryStatus.Posted),
                l => l.JournalEntryId, j => j.Id,
                (l, j) => new
                {
                    j.EntryDate,
                    j.EntryNumber,
                    Description = l.Description ?? j.Description,
                    l.DebitAmount,
                    l.CreditAmount
                });

        // Opening balance: net of all activity strictly before the period start.
        var prior = await ledgerLines
            .Where(x => x.EntryDate < from)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Debit = g.Sum(x => x.DebitAmount),
                Credit = g.Sum(x => x.CreditAmount)
            })
            .FirstOrDefaultAsync(ct);

        var openingBalance = prior is null
            ? 0m
            : isDebitAccount ? prior.Debit - prior.Credit : prior.Credit - prior.Debit;

        var periodRows = await ledgerLines
            .Where(x => x.EntryDate >= from && x.EntryDate <= to)
            .OrderBy(x => x.EntryDate).ThenBy(x => x.EntryNumber)
            .ToListAsync(ct);

        var running = openingBalance;
        decimal totalDebit = 0m, totalCredit = 0m;
        var lines = new List<GeneralLedgerLineDto>(periodRows.Count);

        foreach (var row in periodRows)
        {
            var delta = isDebitAccount
                ? row.DebitAmount - row.CreditAmount
                : row.CreditAmount - row.DebitAmount;
            running += delta;
            totalDebit += row.DebitAmount;
            totalCredit += row.CreditAmount;

            lines.Add(new GeneralLedgerLineDto(
                row.EntryDate, row.EntryNumber, row.Description,
                row.DebitAmount, row.CreditAmount, running));
        }

        return new GeneralLedgerResponse(
            account.Id, account.AccountCode, account.Name, account.NameKa,
            account.AccountType.ToString(), account.BalanceType.ToString(),
            from, to,
            openingBalance, running, totalDebit, totalCredit,
            lines);
    }
}
