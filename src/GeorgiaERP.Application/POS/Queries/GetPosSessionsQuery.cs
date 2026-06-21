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
        var query = _dbContext.PosSessions.AsNoTracking();

        if (request.TerminalId.HasValue)
            query = query.Where(s => s.TerminalId == request.TerminalId.Value);

        if (!string.IsNullOrEmpty(request.Status) &&
            Enum.TryParse<PosSessionStatus>(request.Status, true, out var status))
            query = query.Where(s => s.Status == status);

        var totalCount = await query.CountAsync(cancellationToken);

        // Fetch sessions with related terminal codes and transaction counts.
        // Uses separate queries and client-side ordering for cross-provider
        // compatibility (SQLite does not support DateTimeOffset in ORDER BY).
        var allMatching = await query.ToListAsync(cancellationToken);

        var paged = allMatching
            .OrderByDescending(s => s.OpenedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var sessionIds = paged.Select(s => s.Id).ToList();

        var terminalIds = paged.Select(s => s.TerminalId).Distinct().ToList();
        var terminals = await _dbContext.PosTerminals
            .Where(t => terminalIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Code, cancellationToken);

        var transactionCounts = await _dbContext.PosTransactions
            .Where(t => sessionIds.Contains(t.SessionId))
            .GroupBy(t => t.SessionId)
            .Select(g => new { SessionId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SessionId, x => x.Count, cancellationToken);

        var items = paged.Select(s => new PosSessionSummary(
                s.Id,
                s.TerminalId,
                terminals.GetValueOrDefault(s.TerminalId, ""),
                s.CashierId,
                s.Status.ToString(),
                s.OpeningBalance,
                s.ClosingBalance,
                s.CashDifference,
                transactionCounts.GetValueOrDefault(s.Id, 0),
                s.OpenedAt,
                s.ClosedAt))
            .ToList();

        return new PagedResult<PosSessionSummary>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
