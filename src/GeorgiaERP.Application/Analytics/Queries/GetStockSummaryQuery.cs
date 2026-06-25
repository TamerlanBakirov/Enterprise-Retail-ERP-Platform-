using GeorgiaERP.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Analytics.Queries;

public record GetStockSummaryQuery : IRequest<StockSummaryDto>;

public class GetStockSummaryQueryHandler : IRequestHandler<GetStockSummaryQuery, StockSummaryDto>
{
    private readonly IAppDbContext _dbContext;

    public GetStockSummaryQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<StockSummaryDto> Handle(GetStockSummaryQuery request, CancellationToken ct)
    {
        var stockData = await _dbContext.StockLevels
            .Join(_dbContext.Products, s => s.ProductId, p => p.Id,
                (s, p) => new
                {
                    s.QuantityOnHand,
                    s.CostPrice,
                    IsLowStock = p.MinStockLevel.HasValue && s.QuantityOnHand <= p.MinStockLevel.Value,
                    IsOutOfStock = s.QuantityOnHand <= 0
                })
            .ToListAsync(ct);

        var totalItems = stockData.Count;
        var lowStockItems = stockData.Count(s => s.IsLowStock);
        var outOfStockItems = stockData.Count(s => s.IsOutOfStock);
        var totalValue = stockData.Sum(s => s.QuantityOnHand * s.CostPrice);

        return new StockSummaryDto(totalItems, lowStockItems, outOfStockItems, totalValue);
    }
}
