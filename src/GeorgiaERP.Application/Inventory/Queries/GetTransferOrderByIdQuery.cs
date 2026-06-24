using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Inventory.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Inventory.Queries;

/// <summary>
/// Returns a single transfer order with its lines, resolving source/destination
/// warehouse names and per-line product names so the transfer contents are
/// human-readable — the list query carries neither lines nor names.
/// </summary>
public record GetTransferOrderByIdQuery(Guid Id) : IRequest<Result<TransferOrderDetailDto>>;

public class GetTransferOrderByIdQueryHandler
    : IRequestHandler<GetTransferOrderByIdQuery, Result<TransferOrderDetailDto>>
{
    private readonly IAppDbContext _dbContext;
    public GetTransferOrderByIdQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<TransferOrderDetailDto>> Handle(GetTransferOrderByIdQuery request, CancellationToken ct)
    {
        var order = await _dbContext.TransferOrders.AsNoTracking()
            .Include(t => t.Lines)
            .FirstOrDefaultAsync(t => t.Id == request.Id, ct);

        if (order is null)
            return Result.NotFound<TransferOrderDetailDto>("TransferOrder", request.Id);

        var warehouseIds = new[] { order.SourceWarehouseId, order.DestWarehouseId };
        var warehouseNames = await _dbContext.Warehouses.AsNoTracking()
            .Where(w => warehouseIds.Contains(w.Id))
            .Select(w => new { w.Id, w.Name })
            .ToDictionaryAsync(w => w.Id, w => w.Name, ct);

        var productIds = order.Lines.Select(l => l.ProductId).Distinct().ToList();
        var productNames = productIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _dbContext.Products.AsNoTracking()
                .Where(p => productIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Name })
                .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        var dto = new TransferOrderDetailDto(
            order.Id, order.TransferNumber,
            order.SourceWarehouseId, warehouseNames.GetValueOrDefault(order.SourceWarehouseId),
            order.DestWarehouseId, warehouseNames.GetValueOrDefault(order.DestWarehouseId),
            order.Status.ToString(), order.RsGeWaybillId, order.RequestedBy,
            order.ApprovedBy, order.ShippedAt, order.ReceivedAt, order.Notes, order.CreatedAt,
            order.Lines
                .Select(l => new TransferOrderLineDto(
                    l.Id, l.ProductId, productNames.GetValueOrDefault(l.ProductId),
                    l.VariantId, l.RequestedQty, l.ShippedQty, l.ReceivedQty,
                    l.BatchNumber, l.SerialNumber))
                .ToList());

        return Result.Success(dto);
    }
}
