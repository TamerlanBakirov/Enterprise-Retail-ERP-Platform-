using GeorgiaERP.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.CRM.Commands;

public record UpdateCustomerCommand(
    Guid CustomerId,
    string? FirstName,
    string? LastName,
    string? FirstNameKa,
    string? LastNameKa,
    string? CompanyName,
    string? Tin,
    string? Phone,
    string? Email,
    bool? ConsentSms,
    bool? ConsentEmail,
    bool? IsActive) : IRequest<Result>;

public class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand, Result>
{
    private readonly IAppDbContext _dbContext;

    public UpdateCustomerCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);

        if (customer is null)
            return Result.NotFound("Customer", request.CustomerId);

        if (request.Phone is not null || request.Email is not null)
            customer.SetContactInfo(request.Phone ?? customer.Phone, request.Email ?? customer.Email);

        if (request.CompanyName is not null || request.Tin is not null)
            customer.SetCompany(request.CompanyName ?? customer.CompanyName, request.Tin ?? customer.Tin);

        if (request.ConsentSms.HasValue || request.ConsentEmail.HasValue)
            customer.SetConsent(
                request.ConsentSms ?? customer.ConsentSms,
                request.ConsentEmail ?? customer.ConsentEmail);

        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value) customer.Activate();
            else customer.Deactivate();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
