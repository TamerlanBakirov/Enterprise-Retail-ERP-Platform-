using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.POS;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Analytics.Queries;

public record GetSalesByCategoryQuery : IRequest<List<SalesByCategoryDto>>;

public class GetSalesByCategoryQueryHandler : IRequestHandler<GetSalesByCategoryQuery, List<SalesByCategoryDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetSalesByCategoryQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<List<SalesByCategoryDto>> Handle(GetSalesByCategoryQuery request, CancellationToken ct)
    {
        var completedSaleIds = _dbContext.PosTransactions
            .Where(t => t.Status == PosTransactionStatus.Completed
                     && t.TransactionType == PosTransactionType.Sale)
            .Select(t => t.Id);

        // Join transaction lines with products and categories
        var lineData = await _dbContext.PosTransactionLines
            .Where(l => completedSaleIds.Contains(l.TransactionId))
            .Join(_dbContext.Products, l => l.ProductId, p => p.Id, (l, p) => new { l.LineTotal, p.CategoryId })
            .Join(_dbContext.Categories, x => x.CategoryId, c => c.Id, (x, c) => new { x.LineTotal, CategoryName = c.Name })
            .ToListAsync(ct);

        var totalRevenue = lineData.Sum(x => x.LineTotal);

        var result = lineData
            .GroupBy(x => x.CategoryName)
            .Select(g =>
            {
                var revenue = g.Sum(x => x.LineTotal);
                var percentage = totalRevenue > 0 ? Math.Round(revenue / totalRevenue * 100, 2) : 0;
                return new SalesByCategoryDto(g.Key, revenue, percentage);
            })
            .OrderByDescending(x => x.Revenue)
            .ToList();

        return result;
    }
}
