using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Inventory.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Inventory.Queries;

public record GetStockLevelsQuery(
    Guid? WarehouseId = null,
    Guid? ProductId = null,
    bool LowStockOnly = false,
    string? Search = null,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedResult<StockLevelDto>>;

public class GetStockLevelsQueryHandler : IRequestHandler<GetStockLevelsQuery, PagedResult<StockLevelDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetStockLevelsQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<PagedResult<StockLevelDto>> Handle(GetStockLevelsQuery request, CancellationToken cancellationToken)
    {
        // Join stock levels with products and warehouses for name resolution
        var query = from s in _dbContext.StockLevels.AsNoTracking()
                    join p in _dbContext.Products.AsNoTracking() on s.ProductId equals p.Id
                    join w in _dbContext.Warehouses.AsNoTracking() on s.WarehouseId equals w.Id
                    select new { Stock = s, ProductName = p.Name, WarehouseName = w.Name, MinStockLevel = p.MinStockLevel };

        if (request.WarehouseId.HasValue)
            query = query.Where(x => x.Stock.WarehouseId == request.WarehouseId.Value);

        if (request.ProductId.HasValue)
            query = query.Where(x => x.Stock.ProductId == request.ProductId.Value);

        if (request.LowStockOnly)
            query = query.Where(x => x.MinStockLevel.HasValue && x.Stock.QuantityOnHand <= x.MinStockLevel.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(x => x.ProductName.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.ProductName)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new StockLevelDto(
                x.Stock.Id,
                x.Stock.ProductId,
                x.ProductName,
                x.Stock.VariantId,
                x.Stock.WarehouseId,
                x.WarehouseName,
                x.Stock.LocationCode,
                x.Stock.QuantityOnHand,
                x.Stock.QuantityReserved,
                x.Stock.QuantityInTransit,
                x.Stock.QuantityOnHand - x.Stock.QuantityReserved,
                x.Stock.CostPrice,
                x.Stock.LastCountDate,
                x.Stock.UpdatedAt))
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
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    string? MovementType = null,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedResult<StockMovementDto>>;

public class GetStockMovementsQueryHandler : IRequestHandler<GetStockMovementsQuery, PagedResult<StockMovementDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetStockMovementsQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<PagedResult<StockMovementDto>> Handle(GetStockMovementsQuery request, CancellationToken cancellationToken)
    {
        // Join with products and warehouses to resolve names
        var query = from m in _dbContext.StockMovements.AsNoTracking()
                    join p in _dbContext.Products.AsNoTracking() on m.ProductId equals p.Id
                    join w in _dbContext.Warehouses.AsNoTracking() on m.WarehouseId equals w.Id
                    select new { Movement = m, ProductName = p.Name, WarehouseName = w.Name };

        if (request.WarehouseId.HasValue)
            query = query.Where(x => x.Movement.WarehouseId == request.WarehouseId.Value);

        if (request.ProductId.HasValue)
            query = query.Where(x => x.Movement.ProductId == request.ProductId.Value);

        if (request.From.HasValue)
            query = query.Where(x => x.Movement.CreatedAt >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(x => x.Movement.CreatedAt <= request.To.Value);

        if (!string.IsNullOrEmpty(request.MovementType) &&
            Enum.TryParse<GeorgiaERP.Domain.Inventory.MovementType>(request.MovementType, true, out var movementType))
            query = query.Where(x => x.Movement.MovementType == movementType);

        var totalCount = await query.CountAsync(cancellationToken);

        var rawItems = await query
            .ToListAsync(cancellationToken);

        var items = rawItems
            .OrderByDescending(x => x.Movement.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new StockMovementDto(
                x.Movement.Id,
                x.Movement.MovementType.ToString(),
                x.Movement.ProductId,
                x.ProductName,
                x.Movement.VariantId,
                x.Movement.WarehouseId,
                x.WarehouseName,
                x.Movement.Quantity,
                x.Movement.CostPrice,
                x.Movement.ReferenceType,
                x.Movement.ReferenceId,
                x.Movement.BatchNumber,
                x.Movement.SerialNumber,
                x.Movement.ExpiryDate,
                x.Movement.Notes,
                x.Movement.CreatedAt,
                x.Movement.CreatedBy)).ToList();

        return new PagedResult<StockMovementDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
