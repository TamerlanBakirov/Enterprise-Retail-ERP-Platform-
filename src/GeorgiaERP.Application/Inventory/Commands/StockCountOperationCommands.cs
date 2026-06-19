using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Inventory.Commands;

public record RecordCountLineCommand(Guid StockCountId, Guid LineId, decimal CountedQty, Guid CountedBy) : IRequest<Result>;

public record CompleteStockCountCommand(Guid StockCountId, Guid ApprovedBy) : IRequest<Result<StockCountCompleteResponse>>;

public record StockCountCompleteResponse(int AdjustedLines, int TotalLines);

public class RecordCountLineCommandHandler : IRequestHandler<RecordCountLineCommand, Result>
{
    private readonly IAppDbContext _dbContext;
    public RecordCountLineCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(RecordCountLineCommand request, CancellationToken ct)
    {
        var count = await _dbContext.StockCounts.FirstOrDefaultAsync(c => c.Id == request.StockCountId, ct);
        if (count is null) return Result.Failure("Stock count not found.");
        if (count.Status != StockCountStatus.InProgress) return Result.Failure("Stock count is not in progress.");

        var line = await _dbContext.StockCountLines
            .FirstOrDefaultAsync(l => l.Id == request.LineId && l.StockCountId == request.StockCountId, ct);
        if (line is null) return Result.Failure("Count line not found.");

        line.RecordCount(request.CountedQty, request.CountedBy);
        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class CompleteStockCountCommandHandler
    : IRequestHandler<CompleteStockCountCommand, Result<StockCountCompleteResponse>>
{
    private readonly IAppDbContext _dbContext;
    public CompleteStockCountCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<StockCountCompleteResponse>> Handle(
        CompleteStockCountCommand request, CancellationToken ct)
    {
        var count = await _dbContext.StockCounts
            .Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.Id == request.StockCountId, ct);

        if (count is null) return Result.Failure<StockCountCompleteResponse>("Stock count not found.");
        if (count.Status != StockCountStatus.InProgress)
            return Result.Failure<StockCountCompleteResponse>("Stock count is not in progress.");

        var uncounted = count.Lines.Any(l => l.CountedQty is null);
        if (uncounted) return Result.Failure<StockCountCompleteResponse>("All lines must be counted before completing.");

        int adjusted = 0;
        foreach (var line in count.Lines)
        {
            var variance = line.CountedQty!.Value - line.ExpectedQty;
            if (variance == 0) continue;

            var stock = await _dbContext.StockLevels
                .FirstOrDefaultAsync(s => s.ProductId == line.ProductId
                    && s.WarehouseId == count.WarehouseId
                    && s.VariantId == line.VariantId, ct);

            if (stock is null) continue;

            if (variance > 0)
                stock.AddStock(variance);
            else
                stock.Deduct(Math.Abs(variance));

            _dbContext.StockMovements.Add(StockMovement.Create(
                MovementType.Adjustment, line.ProductId, count.WarehouseId,
                variance, stock.CostPrice, request.ApprovedBy, line.VariantId));

            adjusted++;
        }

        count.Complete(request.ApprovedBy);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(new StockCountCompleteResponse(adjusted, count.Lines.Count));
    }
}
