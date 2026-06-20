using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Procurement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Procurement.Queries;

public record GetSuppliersQuery(
    string? Search = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<SupplierDto>>;

public class GetSuppliersQueryHandler : IRequestHandler<GetSuppliersQuery, PagedResult<SupplierDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetSuppliersQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<PagedResult<SupplierDto>> Handle(GetSuppliersQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Suppliers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(s =>
                s.Code.ToLower().Contains(search) ||
                s.Name.ToLower().Contains(search) ||
                (s.Tin != null && s.Tin.Contains(search)));
        }

        if (request.IsActive.HasValue)
            query = query.Where(s => s.IsActive == request.IsActive.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(s => s.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new SupplierDto(
                s.Id, s.Code, s.Name, s.NameKa, s.Tin, s.IsVatPayer,
                s.ContactPerson, s.Phone, s.Email, s.Address,
                s.PaymentTerms, s.CreditLimit, s.Rating,
                s.IsActive, s.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<SupplierDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}

public record GetPurchaseOrdersQuery(
    Guid? SupplierId = null,
    string? Status = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<PurchaseOrderDto>>;

public class GetPurchaseOrdersQueryHandler : IRequestHandler<GetPurchaseOrdersQuery, PagedResult<PurchaseOrderDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetPurchaseOrdersQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<PagedResult<PurchaseOrderDto>> Handle(GetPurchaseOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.PurchaseOrders.AsQueryable();

        if (request.SupplierId.HasValue)
            query = query.Where(p => p.SupplierId == request.SupplierId.Value);

        if (!string.IsNullOrEmpty(request.Status) &&
            Enum.TryParse<Domain.Procurement.PurchaseOrderStatus>(request.Status, true, out var status))
            query = query.Where(p => p.Status == status);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Include(p => p.Supplier)
            .Include(p => p.Lines)
            .Select(p => new PurchaseOrderDto(
                p.Id, p.PoNumber, p.SupplierId, p.Supplier.Name,
                p.WarehouseId, p.Status.ToString(), p.OrderDate,
                p.ExpectedDate, p.Subtotal, p.VatTotal, p.Total,
                p.Notes, p.CreatedBy, p.CreatedAt,
                p.Lines.Select(l => new PurchaseOrderLineDto(
                    l.Id, l.LineNumber, l.ProductId, null,
                    l.OrderedQty, l.ReceivedQty, l.UnitPrice,
                    l.VatAmount, l.LineTotal)).ToList()))
            .ToListAsync(cancellationToken);

        return new PagedResult<PurchaseOrderDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
