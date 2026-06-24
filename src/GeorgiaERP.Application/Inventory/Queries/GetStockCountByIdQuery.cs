using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Inventory.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Inventory.Queries;

/// <summary>
/// Returns a single stock count with its lines, exposing the per-line variance
/// (counted minus expected) and roll-up totals so a stock-take can be reviewed
/// before adjustments are posted. The list query carries only summary counts.
/// </summary>
public record GetStockCountByIdQuery(Guid Id) : IRequest<Result<StockCountDetailDto>>;

public class GetStockCountByIdQueryHandler
    : IRequestHandler<GetStockCountByIdQuery, Result<StockCountDetailDto>>
{
    private readonly IAppDbContext _dbContext;
    public GetStockCountByIdQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<StockCountDetailDto>> Handle(GetStockCountByIdQuery request, CancellationToken ct)
    {
        var count = await _dbContext.StockCounts.AsNoTracking()
            .Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.Id == request.Id, ct);

        if (count is null)
            return Result.NotFound<StockCountDetailDto>("StockCount", request.Id);

        var warehouse = await _dbContext.Warehouses.AsNoTracking()
            .Where(w => w.Id == count.WarehouseId)
            .Select(w => w.Name)
            .FirstOrDefaultAsync(ct);

        var productIds = count.Lines.Select(l => l.ProductId).Distinct().ToList();
        var productNames = productIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _dbContext.Products.AsNoTracking()
                .Where(p => productIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Name })
                .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        var lines = count.Lines
            .Select(l => new StockCountLineDto(
                l.Id, l.ProductId, productNames.GetValueOrDefault(l.ProductId),
                l.VariantId, l.ExpectedQty, l.CountedQty, l.Variance, l.CountedBy))
            .ToList();

        var dto = new StockCountDetailDto(
            count.Id, count.WarehouseId, warehouse, count.CountType.ToString(),
            count.Status.ToString(), count.StartedAt, count.CompletedAt,
            count.CreatedBy, count.ApprovedBy, count.CreatedAt,
            TotalVariance: lines.Sum(l => l.Variance),
            LinesWithVariance: lines.Count(l => l.Variance != 0),
            Lines: lines);

        return Result.Success(dto);
    }
}
