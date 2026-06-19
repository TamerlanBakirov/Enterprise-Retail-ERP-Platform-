using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Finance;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Finance.Commands;

public record CreateJournalEntryCommand(
    DateTimeOffset EntryDate,
    string? Description,
    string? SourceType,
    Guid? SourceId,
    Guid CreatedBy,
    List<JournalLineInput> Lines) : IRequest<Result<JournalEntryResponse>>;

public record JournalLineInput(Guid AccountId, decimal DebitAmount, decimal CreditAmount, string? Description = null);

public record JournalEntryResponse(Guid Id, string EntryNumber, decimal TotalDebit, decimal TotalCredit);

public class CreateJournalEntryCommandHandler
    : IRequestHandler<CreateJournalEntryCommand, Result<JournalEntryResponse>>
{
    private readonly IAppDbContext _dbContext;
    public CreateJournalEntryCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<JournalEntryResponse>> Handle(CreateJournalEntryCommand request, CancellationToken ct)
    {
        var entryNumber = $"JE-{DateTimeOffset.UtcNow:yyMMdd}-{Random.Shared.Next(10000, 99999)}";
        var entry = JournalEntry.Create(entryNumber, request.EntryDate, request.CreatedBy, request.Description);

        if (request.SourceType is not null)
            entry.SetSource(request.SourceType, request.SourceId);

        decimal totalDebit = 0, totalCredit = 0;
        int lineNum = 1;

        foreach (var input in request.Lines)
        {
            var accountExists = await _dbContext.ChartOfAccounts
                .AnyAsync(a => a.Id == input.AccountId && a.IsActive && !a.IsHeader, ct);
            if (!accountExists) return Result.Failure<JournalEntryResponse>($"Account {input.AccountId} not found or is a header.");

            entry.Lines.Add(JournalEntryLine.Create(
                entry.Id, lineNum++, input.AccountId, input.DebitAmount, input.CreditAmount, input.Description));

            totalDebit += input.DebitAmount;
            totalCredit += input.CreditAmount;
        }

        if (totalDebit != totalCredit)
            return Result.Failure<JournalEntryResponse>(
                $"Journal entry must balance: debit ({totalDebit:F2}) ≠ credit ({totalCredit:F2}).");

        entry.SetTotals(totalDebit, totalCredit);

        _dbContext.JournalEntries.Add(entry);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(new JournalEntryResponse(entry.Id, entryNumber, totalDebit, totalCredit));
    }
}
