using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Inventory.Commands;

public record AdjustStockCommand(
    Guid ProductId,
    Guid WarehouseId,
    decimal Quantity,
    Guid AdjustedBy,
    string? Notes = null,
    Guid? VariantId = null) : IRequest<Result>;

public class AdjustStockCommandHandler : IRequestHandler<AdjustStockCommand, Result>
{
    private readonly IAppDbContext _dbContext;

    public AdjustStockCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(AdjustStockCommand request, CancellationToken ct)
    {
        var stock = await _dbContext.StockLevels
            .FirstOrDefaultAsync(s => s.ProductId == request.ProductId
                && s.WarehouseId == request.WarehouseId
                && s.VariantId == request.VariantId, ct);

        if (stock is null)
            return Result.Failure("Stock level not found for this product/warehouse combination.");

        if (request.Quantity > 0)
            stock.AddStock(request.Quantity);
        else if (request.Quantity < 0)
            stock.Deduct(Math.Abs(request.Quantity));
        else
            return Result.Failure("Adjustment quantity cannot be zero.");

        var movement = StockMovement.Create(
            MovementType.Adjustment, request.ProductId, request.WarehouseId,
            request.Quantity, stock.CostPrice, request.AdjustedBy, request.VariantId);

        _dbContext.StockMovements.Add(movement);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }
}
