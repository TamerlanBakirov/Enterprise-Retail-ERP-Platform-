using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Inventory.DTOs;
using GeorgiaERP.Domain.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Inventory.Queries;

public record GetTransferOrdersQuery(
    Guid? WarehouseId = null,
    string? Status = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<TransferOrderDto>>;

public class GetTransferOrdersQueryHandler
    : IRequestHandler<GetTransferOrdersQuery, PagedResult<TransferOrderDto>>
{
    private readonly IAppDbContext _dbContext;
    public GetTransferOrdersQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<PagedResult<TransferOrderDto>> Handle(
        GetTransferOrdersQuery request, CancellationToken ct)
    {
        var query = _dbContext.TransferOrders.AsQueryable();

        if (request.WarehouseId.HasValue)
            query = query.Where(t => t.SourceWarehouseId == request.WarehouseId || t.DestWarehouseId == request.WarehouseId);

        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<TransferOrderStatus>(request.Status, true, out var status))
            query = query.Where(t => t.Status == status);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new TransferOrderDto(
                t.Id, t.TransferNumber, t.SourceWarehouseId, null,
                t.DestWarehouseId, null, t.Status.ToString(),
                t.RsGeWaybillId, t.RequestedBy, t.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<TransferOrderDto>
        {
            Items = items, TotalCount = totalCount, Page = request.Page, PageSize = request.PageSize
        };
    }
}
