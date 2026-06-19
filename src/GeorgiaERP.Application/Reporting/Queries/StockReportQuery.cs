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

        var stockData = await query.Join(
            _dbContext.Products,
            s => s.ProductId,
            p => p.Id,
            (s, p) => new { Stock = s, Product = p })
            .Select(x => new StockSummaryItem(
                x.Stock.ProductId,
                x.Product.Name,
                x.Stock.WarehouseId,
                x.Stock.QuantityOnHand,
                x.Stock.QuantityReserved,
                x.Stock.QuantityOnHand - x.Stock.QuantityReserved,
                x.Stock.CostPrice,
                x.Stock.QuantityOnHand * x.Stock.CostPrice,
                x.Product.MinStockLevel.HasValue && x.Stock.QuantityOnHand <= x.Product.MinStockLevel.Value))
            .OrderBy(x => x.ProductName)
            .ToListAsync(ct);

        return new StockReport(
            stockData.Sum(s => s.StockValue),
            stockData.Select(s => s.ProductId).Distinct().Count(),
            stockData.Count(s => s.IsLowStock),
            stockData.Count(s => s.QuantityOnHand <= 0),
            stockData);
    }
}
