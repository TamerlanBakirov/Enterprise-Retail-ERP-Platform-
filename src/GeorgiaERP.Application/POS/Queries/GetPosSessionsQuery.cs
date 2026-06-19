using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.POS;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.POS.Queries;

public record GetPosSessionsQuery(
    Guid? TerminalId = null,
    string? Status = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<PosSessionSummary>>;

public record PosSessionSummary(
    Guid Id,
    Guid TerminalId,
    string TerminalCode,
    Guid CashierId,
    string Status,
    decimal OpeningBalance,
    decimal? ClosingBalance,
    decimal? CashDifference,
    int TransactionCount,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt);

public class GetPosSessionsQueryHandler
    : IRequestHandler<GetPosSessionsQuery, PagedResult<PosSessionSummary>>
{
    private readonly IAppDbContext _dbContext;

    public GetPosSessionsQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<PosSessionSummary>> Handle(
        GetPosSessionsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.PosSessions.Include(s => s.Terminal).AsQueryable();

        if (request.TerminalId.HasValue)
            query = query.Where(s => s.TerminalId == request.TerminalId.Value);

        if (!string.IsNullOrEmpty(request.Status) &&
            Enum.TryParse<PosSessionStatus>(request.Status, true, out var status))
            query = query.Where(s => s.Status == status);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(s => s.OpenedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new PosSessionSummary(
                s.Id,
                s.TerminalId,
                s.Terminal.Code,
                s.CashierId,
                s.Status.ToString(),
                s.OpeningBalance,
                s.ClosingBalance,
                s.CashDifference,
                s.Transactions.Count,
                s.OpenedAt,
                s.ClosedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<PosSessionSummary>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
