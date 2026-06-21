using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Warehouse.DTOs;
using GeorgiaERP.Domain.Warehouse;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Warehouse.Queries;

public record GetWarehouseByIdQuery(Guid Id) : IRequest<Result<WarehouseDetailDto>>;

public class GetWarehouseByIdQueryHandler : IRequestHandler<GetWarehouseByIdQuery, Result<WarehouseDetailDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetWarehouseByIdQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<WarehouseDetailDto>> Handle(GetWarehouseByIdQuery request, CancellationToken ct)
    {
        var warehouse = await _dbContext.Warehouses
            .Where(w => w.Id == request.Id)
            .Select(w => new WarehouseDetailDto(
                w.Id, w.Code, w.Name, w.NameKa,
                w.WarehouseType.ToString(), w.Address, w.City, w.Region,
                w.LinkedStoreId, w.IsActive, w.CreatedAt,
                _dbContext.WarehouseLocations.Count(l => l.WarehouseId == w.Id)))
            .FirstOrDefaultAsync(ct);

        if (warehouse is null) return Result.NotFound<WarehouseDetailDto>("Warehouse", request.Id);
        return Result.Success(warehouse);
    }
}

public record GetWarehouseLocationsQuery(
    Guid WarehouseId,
    string? LocationType = null,
    bool? IsActive = null) : IRequest<IReadOnlyList<WarehouseLocationDto>>;

public class GetWarehouseLocationsQueryHandler
    : IRequestHandler<GetWarehouseLocationsQuery, IReadOnlyList<WarehouseLocationDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetWarehouseLocationsQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<WarehouseLocationDto>> Handle(
        GetWarehouseLocationsQuery request, CancellationToken ct)
    {
        var query = _dbContext.WarehouseLocations
            .Where(l => l.WarehouseId == request.WarehouseId);

        if (!string.IsNullOrEmpty(request.LocationType) &&
            Enum.TryParse<LocationType>(request.LocationType, true, out var locType))
            query = query.Where(l => l.LocationType == locType);

        if (request.IsActive.HasValue)
            query = query.Where(l => l.IsActive == request.IsActive.Value);

        return await query
            .OrderBy(l => l.SortOrder).ThenBy(l => l.Code)
            .Select(l => new WarehouseLocationDto(
                l.Id, l.WarehouseId, l.Code, l.Name, l.NameKa,
                l.LocationType.ToString(), l.ParentLocationId,
                l.SortOrder, l.IsActive, l.MaxCapacity, l.Notes, l.CreatedAt))
            .ToListAsync(ct);
    }
}

public record GetReceivingOrdersQuery(
    Guid? WarehouseId = null,
    string? Status = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<ReceivingOrderDto>>;

public class GetReceivingOrdersQueryHandler
    : IRequestHandler<GetReceivingOrdersQuery, PagedResult<ReceivingOrderDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetReceivingOrdersQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<PagedResult<ReceivingOrderDto>> Handle(
        GetReceivingOrdersQuery request, CancellationToken ct)
    {
        var query = _dbContext.ReceivingOrders.AsQueryable();

        if (request.WarehouseId.HasValue)
            query = query.Where(r => r.WarehouseId == request.WarehouseId.Value);

        if (!string.IsNullOrEmpty(request.Status) &&
            Enum.TryParse<ReceivingOrderStatus>(request.Status, true, out var status))
            query = query.Where(r => r.Status == status);

        var totalCount = await query.CountAsync(ct);

        var pageSize = Math.Min(request.PageSize, 100);
        var rawItems = await query
            .Include(r => r.Lines)
            .ToListAsync(ct);

        var items = rawItems
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.Page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ReceivingOrderDto(
                r.Id, r.ReceivingNumber, r.WarehouseId,
                r.Status.ToString(), r.Source.ToString(),
                r.SourceOrderId, r.SupplierId, r.ExpectedDate,
                r.ReceivedAt, r.ReceivedBy, r.LocationId, r.Notes, r.CreatedAt,
                r.Lines.Select(l => new ReceivingOrderLineDto(
                    l.Id, l.ProductId, l.VariantId,
                    l.ExpectedQty, l.ReceivedQty, l.DamagedQty,
                    l.BatchNumber, l.SerialNumber, l.LocationId, l.Notes)).ToList()))
            .ToList();

        return new PagedResult<ReceivingOrderDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}

public record GetReceivingOrderByIdQuery(Guid Id) : IRequest<Result<ReceivingOrderDto>>;

public class GetReceivingOrderByIdQueryHandler
    : IRequestHandler<GetReceivingOrderByIdQuery, Result<ReceivingOrderDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetReceivingOrderByIdQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<ReceivingOrderDto>> Handle(GetReceivingOrderByIdQuery request, CancellationToken ct)
    {
        var order = await _dbContext.ReceivingOrders
            .Where(r => r.Id == request.Id)
            .Select(r => new ReceivingOrderDto(
                r.Id, r.ReceivingNumber, r.WarehouseId,
                r.Status.ToString(), r.Source.ToString(),
                r.SourceOrderId, r.SupplierId, r.ExpectedDate,
                r.ReceivedAt, r.ReceivedBy, r.LocationId, r.Notes, r.CreatedAt,
                r.Lines.Select(l => new ReceivingOrderLineDto(
                    l.Id, l.ProductId, l.VariantId,
                    l.ExpectedQty, l.ReceivedQty, l.DamagedQty,
                    l.BatchNumber, l.SerialNumber, l.LocationId, l.Notes)).ToList()))
            .FirstOrDefaultAsync(ct);

        if (order is null) return Result.NotFound<ReceivingOrderDto>("ReceivingOrder", request.Id);
        return Result.Success(order);
    }
}

public record GetShippingOrdersQuery(
    Guid? WarehouseId = null,
    string? Status = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<ShippingOrderDto>>;

public class GetShippingOrdersQueryHandler
    : IRequestHandler<GetShippingOrdersQuery, PagedResult<ShippingOrderDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetShippingOrdersQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<PagedResult<ShippingOrderDto>> Handle(
        GetShippingOrdersQuery request, CancellationToken ct)
    {
        var query = _dbContext.ShippingOrders.AsQueryable();

        if (request.WarehouseId.HasValue)
            query = query.Where(s => s.WarehouseId == request.WarehouseId.Value);

        if (!string.IsNullOrEmpty(request.Status) &&
            Enum.TryParse<ShippingOrderStatus>(request.Status, true, out var status))
            query = query.Where(s => s.Status == status);

        var totalCount = await query.CountAsync(ct);

        var pageSize = Math.Min(request.PageSize, 100);
        var rawItems = await query
            .Include(s => s.Lines)
            .ToListAsync(ct);

        var items = rawItems
            .OrderByDescending(s => s.CreatedAt)
            .Skip((request.Page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new ShippingOrderDto(
                s.Id, s.ShippingNumber, s.WarehouseId,
                s.Status.ToString(), s.OrderType.ToString(),
                s.SourceOrderId, s.CustomerId, s.DestWarehouseId,
                s.ShippingAddress, s.TrackingNumber, s.Carrier,
                s.ExpectedShipDate, s.ShippedAt, s.DeliveredAt,
                s.ShippedBy, s.RsGeWaybillId, s.Notes, s.CreatedAt,
                s.Lines.Select(l => new ShippingOrderLineDto(
                    l.Id, l.ProductId, l.VariantId,
                    l.OrderedQty, l.PickedQty, l.PackedQty, l.ShippedQty,
                    l.PickLocationId, l.BatchNumber, l.SerialNumber, l.Notes)).ToList()))
            .ToList();

        return new PagedResult<ShippingOrderDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}

public record GetShippingOrderByIdQuery(Guid Id) : IRequest<Result<ShippingOrderDto>>;

public class GetShippingOrderByIdQueryHandler
    : IRequestHandler<GetShippingOrderByIdQuery, Result<ShippingOrderDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetShippingOrderByIdQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<ShippingOrderDto>> Handle(GetShippingOrderByIdQuery request, CancellationToken ct)
    {
        var order = await _dbContext.ShippingOrders
            .Where(s => s.Id == request.Id)
            .Select(s => new ShippingOrderDto(
                s.Id, s.ShippingNumber, s.WarehouseId,
                s.Status.ToString(), s.OrderType.ToString(),
                s.SourceOrderId, s.CustomerId, s.DestWarehouseId,
                s.ShippingAddress, s.TrackingNumber, s.Carrier,
                s.ExpectedShipDate, s.ShippedAt, s.DeliveredAt,
                s.ShippedBy, s.RsGeWaybillId, s.Notes, s.CreatedAt,
                s.Lines.Select(l => new ShippingOrderLineDto(
                    l.Id, l.ProductId, l.VariantId,
                    l.OrderedQty, l.PickedQty, l.PackedQty, l.ShippedQty,
                    l.PickLocationId, l.BatchNumber, l.SerialNumber, l.Notes)).ToList()))
            .FirstOrDefaultAsync(ct);

        if (order is null) return Result.NotFound<ShippingOrderDto>("ShippingOrder", request.Id);
        return Result.Success(order);
    }
}
