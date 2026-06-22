using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.CRM.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.CRM.Queries;

public record GetCustomerByIdQuery(Guid CustomerId) : IRequest<Result<CustomerDto>>;

public class GetCustomerByIdQueryHandler : IRequestHandler<GetCustomerByIdQuery, Result<CustomerDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetCustomerByIdQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<CustomerDto>> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var c = await _dbContext.Customers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);

        if (c is null)
            return Result.NotFound<CustomerDto>("Customer", request.CustomerId);

        return Result.Success(new CustomerDto(
            c.Id, c.CustomerNumber, c.FirstName, c.LastName,
            c.FirstNameKa, c.LastNameKa, c.CompanyName, c.Tin,
            c.Phone, c.Email, c.LoyaltyCardNumber, c.LoyaltyTier,
            c.LoyaltyPoints, c.TotalPurchases, c.TotalVisits,
            c.LastVisitAt, c.IsActive, c.CreatedAt));
    }
}
