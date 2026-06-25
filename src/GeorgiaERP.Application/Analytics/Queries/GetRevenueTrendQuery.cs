using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.POS;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Analytics.Queries;

public record GetRevenueTrendQuery(int Days = 30) : IRequest<List<RevenueTrendPoint>>;

public class GetRevenueTrendQueryHandler : IRequestHandler<GetRevenueTrendQuery, List<RevenueTrendPoint>>
{
    private readonly IAppDbContext _dbContext;

    public GetRevenueTrendQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<List<RevenueTrendPoint>> Handle(GetRevenueTrendQuery request, CancellationToken ct)
    {
        var today = DateTimeOffset.UtcNow.Date;

        // Load all completed sales into memory then filter by date to avoid SQLite DateTimeOffset translation issues
        var allTransactions = await _dbContext.PosTransactions
            .Where(t => t.Status == PosTransactionStatus.Completed
                     && t.TransactionType == PosTransactionType.Sale)
            .Select(t => new { t.CreatedAt, t.Total })
            .ToListAsync(ct);

        var startDate = today.AddDays(-(request.Days - 1));
        var transactions = allTransactions
            .Where(t => t.CreatedAt.Date >= startDate)
            .ToList();

        var grouped = transactions
            .GroupBy(t => t.CreatedAt.Date)
            .Select(g => new RevenueTrendPoint(
                g.Key.ToString("yyyy-MM-dd"),
                g.Sum(t => t.Total),
                g.Count()))
            .OrderBy(r => r.Date)
            .ToList();

        return grouped;
    }
}
