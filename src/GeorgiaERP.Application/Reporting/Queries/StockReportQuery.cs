using GeorgiaERP.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Reporting.Queries;

public record StockReportQuery(Guid? WarehouseId = null) : IRequest<StockReport>;

public record StockReport(
    decimal TotalStockValue,
    int TotalProducts,
    int LowStockProducts,
    int OutOfStockProducts,
    List<StockSummaryItem> Items);

public record StockSummaryItem(
    Guid ProductId,
    string ProductName,
    Guid WarehouseId,
    decimal QuantityOnHand,
    decimal QuantityReserved,
    decimal Available,
    decimal CostPrice,
    decimal StockValue,
    bool IsLowStock);

public class StockReportQueryHandler : IRequestHandler<StockReportQuery, StockReport>
{
    private readonly IAppDbContext _dbContext;
    public StockReportQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<StockReport> Handle(StockReportQuery request, CancellationToken ct)
    {
        var query = _dbContext.StockLevels.AsQueryable();
        if (request.WarehouseId.HasValue)
            query = query.Where(s => s.WarehouseId == request.WarehouseId.Value);

        var rawData = await query.Join(
            _dbContext.Products,
            s => s.ProductId,
            p => p.Id,
            (s, p) => new
            {
                s.ProductId,
                ProductName = p.Name,
                s.WarehouseId,
                s.QuantityOnHand,
                s.QuantityReserved,
                s.CostPrice,
                p.MinStockLevel
            })
            .OrderBy(x => x.ProductName)
            .ToListAsync(ct);

        var stockData = rawData.Select(x => new StockSummaryItem(
                x.ProductId,
                x.ProductName,
                x.WarehouseId,
                x.QuantityOnHand,
                x.QuantityReserved,
                x.QuantityOnHand - x.QuantityReserved,
                x.CostPrice,
                x.QuantityOnHand * x.CostPrice,
                x.MinStockLevel.HasValue && x.QuantityOnHand <= x.MinStockLevel.Value))
            .ToList();

        return new StockReport(
            stockData.Sum(s => s.StockValue),
            stockData.Select(s => s.ProductId).Distinct().Count(),
            stockData.Count(s => s.IsLowStock),
            stockData.Count(s => s.QuantityOnHand <= 0),
            stockData);
    }
}
