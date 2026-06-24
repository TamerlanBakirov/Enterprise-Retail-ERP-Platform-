using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Procurement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Procurement.Queries;

/// <summary>
/// Returns a single purchase order with its supplier and line detail, resolving
/// product names so the line items are human-readable — unlike the list query,
/// which leaves product names null for payload size.
/// </summary>
public record GetPurchaseOrderByIdQuery(Guid Id) : IRequest<Result<PurchaseOrderDto>>;

public class GetPurchaseOrderByIdQueryHandler : IRequestHandler<GetPurchaseOrderByIdQuery, Result<PurchaseOrderDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetPurchaseOrderByIdQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<PurchaseOrderDto>> Handle(GetPurchaseOrderByIdQuery request, CancellationToken ct)
    {
        var po = await _dbContext.PurchaseOrders.AsNoTracking()
            .Include(p => p.Supplier)
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == request.Id, ct);

        if (po is null)
            return Result.NotFound<PurchaseOrderDto>("PurchaseOrder", request.Id);

        var productIds = po.Lines.Select(l => l.ProductId).Distinct().ToList();
        var productNames = productIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _dbContext.Products.AsNoTracking()
                .Where(p => productIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Name })
                .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        var dto = new PurchaseOrderDto(
            po.Id, po.PoNumber, po.SupplierId, po.Supplier.Name,
            po.WarehouseId, po.Status.ToString(), po.OrderDate,
            po.ExpectedDate, po.Subtotal, po.VatTotal, po.Total,
            po.Notes, po.CreatedBy, po.CreatedAt,
            po.Lines
                .OrderBy(l => l.LineNumber)
                .Select(l => new PurchaseOrderLineDto(
                    l.Id, l.LineNumber, l.ProductId,
                    productNames.GetValueOrDefault(l.ProductId),
                    l.OrderedQty, l.ReceivedQty, l.UnitPrice,
                    l.VatAmount, l.LineTotal))
                .ToList());

        return Result.Success(dto);
    }
}
