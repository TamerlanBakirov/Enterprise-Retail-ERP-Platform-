using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Pricing.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Pricing.Queries;

public record GetPriceListsQuery(
    string? PriceType,
    bool? IsActive,
    int Page,
    int PageSize) : IRequest<PagedResult<PriceListDto>>;

public class GetPriceListsQueryHandler : IRequestHandler<GetPriceListsQuery, PagedResult<PriceListDto>>
{
    private readonly IAppDbContext _db;

    public GetPriceListsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PagedResult<PriceListDto>> Handle(GetPriceListsQuery request, CancellationToken ct)
    {
        var query = _db.PriceLists.AsQueryable();

        if (!string.IsNullOrEmpty(request.PriceType))
            query = query.Where(p => p.PriceType.ToString() == request.PriceType);
        if (request.IsActive.HasValue)
            query = query.Where(p => p.IsActive == request.IsActive.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderBy(p => p.Code)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new PriceListDto(
                p.Id, p.Code, p.Name, p.NameKa, p.Currency,
                p.PriceType.ToString(), p.StoreId,
                p.ValidFrom, p.ValidTo, p.IsActive, p.Priority,
                p.Items.Count, p.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<PriceListDto>
        {
            Items = items, TotalCount = totalCount,
            Page = request.Page, PageSize = request.PageSize
        };
    }
}
