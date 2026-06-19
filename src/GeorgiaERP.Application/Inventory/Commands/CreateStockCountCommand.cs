using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Inventory.Commands;

public record CreateStockCountCommand(
    Guid WarehouseId,
    string CountType,
    Guid CreatedBy,
    List<Guid>? ProductIds = null) : IRequest<Result<StockCountResponse>>;

public record StockCountResponse(Guid Id, string Status, int LineCount);

public class CreateStockCountCommandHandler
    : IRequestHandler<CreateStockCountCommand, Result<StockCountResponse>>
{
    private readonly IAppDbContext _dbContext;

    public CreateStockCountCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<StockCountResponse>> Handle(
        CreateStockCountCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<CountType>(request.CountType, true, out var countType))
            return Result.Failure<StockCountResponse>("Invalid count type.");

        var warehouseExists = await _dbContext.Warehouses.AnyAsync(w => w.Id == request.WarehouseId && w.IsActive, ct);
        if (!warehouseExists) return Result.Failure<StockCountResponse>("Warehouse not found.");

        var stockCount = StockCount.Create(request.WarehouseId, countType, request.CreatedBy);

        var stockQuery = _dbContext.StockLevels.Where(s => s.WarehouseId == request.WarehouseId);

        if (request.ProductIds is { Count: > 0 })
            stockQuery = stockQuery.Where(s => request.ProductIds.Contains(s.ProductId));

        var stockLevels = await stockQuery.ToListAsync(ct);

        foreach (var stock in stockLevels)
        {
            stockCount.Lines.Add(StockCountLine.Create(
                stockCount.Id, stock.ProductId, stock.QuantityOnHand, stock.VariantId));
        }

        stockCount.Start();

        _dbContext.StockCounts.Add(stockCount);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(new StockCountResponse(stockCount.Id, stockCount.Status.ToString(), stockCount.Lines.Count));
    }
}
