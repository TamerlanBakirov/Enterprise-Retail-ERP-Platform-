using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Inventory.Queries;

public record GetStockCountsQuery(
    Guid? WarehouseId = null,
    string? Status = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<StockCountDto>>;

public record StockCountDto(
    Guid Id,
    Guid WarehouseId,
    string CountType,
    string Status,
    int LineCount,
    int CountedLines,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset CreatedAt);

public class GetStockCountsQueryHandler
    : IRequestHandler<GetStockCountsQuery, PagedResult<StockCountDto>>
{
    private readonly IAppDbContext _dbContext;
    public GetStockCountsQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<PagedResult<StockCountDto>> Handle(
        GetStockCountsQuery request, CancellationToken ct)
    {
        var query = _dbContext.StockCounts.AsQueryable();

        if (request.WarehouseId.HasValue)
            query = query.Where(c => c.WarehouseId == request.WarehouseId);

        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<StockCountStatus>(request.Status, true, out var status))
            query = query.Where(c => c.Status == status);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new StockCountDto(
                c.Id, c.WarehouseId, c.CountType.ToString(), c.Status.ToString(),
                c.Lines.Count, c.Lines.Count(l => l.CountedQty != null),
                c.StartedAt, c.CompletedAt, c.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<StockCountDto>
        {
            Items = items, TotalCount = totalCount, Page = request.Page, PageSize = request.PageSize
        };
    }
}
