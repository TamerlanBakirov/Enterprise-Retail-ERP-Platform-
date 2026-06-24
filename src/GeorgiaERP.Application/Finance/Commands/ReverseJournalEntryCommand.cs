using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Finance;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Finance.Commands;

/// <summary>
/// Reverses a posted journal entry by creating and posting a mirror entry with
/// debit and credit amounts swapped, then marking the original as Reversed.
/// </summary>
public record ReverseJournalEntryCommand(
    Guid JournalEntryId,
    Guid ReversedBy,
    string? Reason = null) : IRequest<Result<JournalEntryResponse>>;

public class ReverseJournalEntryCommandHandler
    : IRequestHandler<ReverseJournalEntryCommand, Result<JournalEntryResponse>>
{
    private readonly IAppDbContext _dbContext;
    public ReverseJournalEntryCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<JournalEntryResponse>> Handle(ReverseJournalEntryCommand request, CancellationToken ct)
    {
        var original = await _dbContext.JournalEntries
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Id == request.JournalEntryId, ct);

        if (original is null)
            return Result.Failure<JournalEntryResponse>("Journal entry not found.");
        if (original.Status != JournalEntryStatus.Posted)
            return Result.Failure<JournalEntryResponse>("Only posted entries can be reversed.");

        var entryNumber = $"JE-REV-{DateTimeOffset.UtcNow:yyMMdd}-{Random.Shared.Next(10000, 99999)}";
        var description = string.IsNullOrWhiteSpace(request.Reason)
            ? $"Reversal of {original.EntryNumber}"
            : $"Reversal of {original.EntryNumber}: {request.Reason}";

        var reversal = JournalEntry.Create(entryNumber, DateTimeOffset.UtcNow, request.ReversedBy, description);
        reversal.SetSource("Reversal", original.Id);

        var lineNum = 1;
        foreach (var line in original.Lines.OrderBy(l => l.LineNumber))
        {
            // Swap debit and credit to mirror the original.
            reversal.Lines.Add(JournalEntryLine.Create(
                reversal.Id, lineNum++, line.AccountId,
                line.CreditAmount, line.DebitAmount, line.Description));
        }

        reversal.SetTotals(original.TotalCredit, original.TotalDebit);
        reversal.Post(request.ReversedBy);

        original.Reverse(reversal.Id);

        _dbContext.JournalEntries.Add(reversal);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(new JournalEntryResponse(
            reversal.Id, entryNumber, reversal.TotalDebit, reversal.TotalCredit));
    }
}
