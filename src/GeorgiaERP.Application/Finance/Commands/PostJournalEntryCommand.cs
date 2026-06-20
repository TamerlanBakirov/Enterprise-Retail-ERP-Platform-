using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Finance;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Finance.Commands;

public record PostJournalEntryCommand(Guid JournalEntryId, Guid PostedBy) : IRequest<Result>;

public class PostJournalEntryCommandHandler : IRequestHandler<PostJournalEntryCommand, Result>
{
    private readonly IAppDbContext _dbContext;
    public PostJournalEntryCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(PostJournalEntryCommand request, CancellationToken ct)
    {
        var entry = await _dbContext.JournalEntries.FirstOrDefaultAsync(e => e.Id == request.JournalEntryId, ct);
        if (entry is null) return Result.Failure("Journal entry not found.");
        if (entry.Status != JournalEntryStatus.Draft) return Result.Failure("Only draft entries can be posted.");
        entry.Post(request.PostedBy);
        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}
