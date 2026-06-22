using GeorgiaERP.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Inventory.Queries;

public record ExportStockQuery(Guid? WarehouseId) : IRequest<byte[]>;

public class ExportStockQueryHandler : IRequestHandler<ExportStockQuery, byte[]>
{
    private readonly IAppDbContext _dbContext;
    private readonly IExcelService _excelService;

    public ExportStockQueryHandler(IAppDbContext dbContext, IExcelService excelService)
    {
        _dbContext = dbContext;
        _excelService = excelService;
    }

    public async Task<byte[]> Handle(ExportStockQuery request, CancellationToken ct)
    {
        var query = _dbContext.StockLevels.AsQueryable();

        if (request.WarehouseId.HasValue)
            query = query.Where(s => s.WarehouseId == request.WarehouseId.Value);

        var rows = await query
            .Join(_dbContext.Products, s => s.ProductId, p => p.Id, (s, p) => new { Stock = s, Product = p })
            .Join(_dbContext.Warehouses, x => x.Stock.WarehouseId, w => w.Id, (x, w) => new { x.Stock, x.Product, Warehouse = w })
            .Select(x => new StockExportRow(
                x.Product.Sku,
                x.Product.Name,
                x.Warehouse.Name,
                x.Stock.QuantityOnHand,
                x.Stock.QuantityReserved,
                x.Stock.QuantityOnHand - x.Stock.QuantityReserved,
                x.Stock.CostPrice,
                x.Stock.QuantityOnHand * x.Stock.CostPrice,
                x.Product.MinStockLevel.HasValue && x.Stock.QuantityOnHand <= x.Product.MinStockLevel.Value))
            .OrderBy(x => x.ProductName)
            .ToListAsync(ct);

        return _excelService.ExportStockLevels(rows);
    }
}
