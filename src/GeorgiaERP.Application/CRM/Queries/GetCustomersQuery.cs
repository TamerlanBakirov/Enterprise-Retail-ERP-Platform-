using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.CRM.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.CRM.Queries;

public record GetCustomersQuery(
    string? Search = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<CustomerDto>>;

public class GetCustomersQueryHandler : IRequestHandler<GetCustomersQuery, PagedResult<CustomerDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetCustomersQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<PagedResult<CustomerDto>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Customers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(c =>
                c.FirstName.ToLower().Contains(search) ||
                c.LastName.ToLower().Contains(search) ||
                c.CustomerNumber.Contains(search) ||
                (c.Phone != null && c.Phone.Contains(search)) ||
                (c.LoyaltyCardNumber != null && c.LoyaltyCardNumber.Contains(search)));
        }

        if (request.IsActive.HasValue)
            query = query.Where(c => c.IsActive == request.IsActive.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CustomerDto(
                c.Id, c.CustomerNumber, c.FirstName, c.LastName,
                c.FirstNameKa, c.LastNameKa, c.CompanyName, c.Tin,
                c.Phone, c.Email, c.LoyaltyCardNumber, c.LoyaltyTier,
                c.LoyaltyPoints, c.TotalPurchases, c.TotalVisits,
                c.LastVisitAt, c.IsActive, c.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<CustomerDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
