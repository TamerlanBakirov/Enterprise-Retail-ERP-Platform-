using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Pricing.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Pricing.Queries;

public record GetPromotionsQuery(
    bool? IsActive,
    int Page,
    int PageSize) : IRequest<PagedResult<PromotionDto>>;

public class GetPromotionsQueryHandler : IRequestHandler<GetPromotionsQuery, PagedResult<PromotionDto>>
{
    private readonly IAppDbContext _db;

    public GetPromotionsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PagedResult<PromotionDto>> Handle(GetPromotionsQuery request, CancellationToken ct)
    {
        var query = _db.Promotions.AsNoTracking();

        if (request.IsActive.HasValue)
            query = query.Where(p => p.IsActive == request.IsActive.Value);

        var totalCount = await query.CountAsync(ct);
        var rawItems = await query.ToListAsync(ct);

        var items = rawItems
            .OrderByDescending(p => p.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new PromotionDto(
                p.Id, p.Code, p.Name, p.NameKa,
                p.PromotionType.ToString(), p.DiscountValue, p.Conditions,
                p.ValidFrom, p.ValidTo, p.IsActive,
                p.MaxUses, p.CurrentUses, p.CreatedAt))
            .ToList();

        return new PagedResult<PromotionDto>
        {
            Items = items, TotalCount = totalCount,
            Page = request.Page, PageSize = request.PageSize
        };
    }
}
