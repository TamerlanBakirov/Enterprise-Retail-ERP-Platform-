using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Pricing.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Pricing.Queries;

public record GetPriceListItemsQuery(
    Guid PriceListId,
    string? Search,
    int Page,
    int PageSize) : IRequest<PagedResult<PriceListItemDto>>;

public class GetPriceListItemsQueryHandler : IRequestHandler<GetPriceListItemsQuery, PagedResult<PriceListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetPriceListItemsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PagedResult<PriceListItemDto>> Handle(GetPriceListItemsQuery request, CancellationToken ct)
    {
        var query = _db.PriceListItems
            .AsNoTracking()
            .Where(i => i.PriceListId == request.PriceListId);

        if (!string.IsNullOrEmpty(request.Search))
        {
            var productIds = _db.Products
                .Where(p => p.Name.Contains(request.Search) || p.Sku.Contains(request.Search))
                .Select(p => p.Id);
            query = query.Where(i => productIds.Contains(i.ProductId));
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderBy(i => i.ProductId)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Join(_db.Products, i => i.ProductId, p => p.Id, (i, p) => new PriceListItemDto(
                i.Id, i.PriceListId, i.ProductId, p.Name,
                i.VariantId, i.Price, i.MinQty))
            .ToListAsync(ct);

        return new PagedResult<PriceListItemDto>
        {
            Items = items, TotalCount = totalCount,
            Page = request.Page, PageSize = request.PageSize
        };
    }
}
