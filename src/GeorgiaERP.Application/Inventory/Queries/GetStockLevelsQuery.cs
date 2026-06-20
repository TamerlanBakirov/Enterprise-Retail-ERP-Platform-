using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Inventory.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Inventory.Queries;

public record GetStockLevelsQuery(
    Guid? WarehouseId = null,
    Guid? ProductId = null,
    bool LowStockOnly = false,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedResult<StockLevelDto>>;

public class GetStockLevelsQueryHandler : IRequestHandler<GetStockLevelsQuery, PagedResult<StockLevelDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetStockLevelsQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<PagedResult<StockLevelDto>> Handle(GetStockLevelsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.StockLevels.AsQueryable();

        if (request.WarehouseId.HasValue)
            query = query.Where(s => s.WarehouseId == request.WarehouseId.Value);

        if (request.ProductId.HasValue)
            query = query.Where(s => s.ProductId == request.ProductId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(s => s.ProductId)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new StockLevelDto(
                s.Id,
                s.ProductId,
                null,
                s.VariantId,
                s.WarehouseId,
                null,
                s.LocationCode,
                s.QuantityOnHand,
                s.QuantityReserved,
                s.QuantityInTransit,
                s.QuantityOnHand - s.QuantityReserved,
                s.CostPrice,
                s.LastCountDate,
                s.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<StockLevelDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}

public record GetStockMovementsQuery(
    Guid? WarehouseId = null,
    Guid? ProductId = null,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedResult<StockMovementDto>>;

public class GetStockMovementsQueryHandler : IRequestHandler<GetStockMovementsQuery, PagedResult<StockMovementDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetStockMovementsQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<PagedResult<StockMovementDto>> Handle(GetStockMovementsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.StockMovements.AsQueryable();

        if (request.WarehouseId.HasValue)
            query = query.Where(m => m.WarehouseId == request.WarehouseId.Value);

        if (request.ProductId.HasValue)
            query = query.Where(m => m.ProductId == request.ProductId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(m => new StockMovementDto(
                m.Id,
                m.MovementType.ToString(),
                m.ProductId,
                null,
                m.VariantId,
                m.WarehouseId,
                null,
                m.Quantity,
                m.CostPrice,
                m.ReferenceType,
                m.ReferenceId,
                m.BatchNumber,
                m.SerialNumber,
                m.ExpiryDate,
                m.Notes,
                m.CreatedAt,
                m.CreatedBy))
            .ToListAsync(cancellationToken);

        return new PagedResult<StockMovementDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
