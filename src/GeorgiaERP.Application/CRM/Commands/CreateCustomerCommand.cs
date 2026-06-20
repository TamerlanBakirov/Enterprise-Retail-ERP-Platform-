using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.CRM;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.CRM.Commands;

public record CreateCustomerCommand(
    string FirstName,
    string LastName,
    string? FirstNameKa,
    string? LastNameKa,
    string? CompanyName,
    string? Tin,
    string? Phone,
    string? Email,
    DateTimeOffset? DateOfBirth,
    string? Gender,
    bool ConsentSms,
    bool ConsentEmail) : IRequest<Result<CustomerCreatedResponse>>;

public record CustomerCreatedResponse(Guid Id, string CustomerNumber);

public class CreateCustomerCommandHandler
    : IRequestHandler<CreateCustomerCommand, Result<CustomerCreatedResponse>>
{
    private readonly IAppDbContext _dbContext;
    public CreateCustomerCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<CustomerCreatedResponse>> Handle(CreateCustomerCommand request, CancellationToken ct)
    {
        if (request.Phone is not null)
        {
            var phoneExists = await _dbContext.Customers.AnyAsync(c => c.Phone == request.Phone, ct);
            if (phoneExists) return Result.Failure<CustomerCreatedResponse>("Phone number already registered.");
        }

        var customerNumber = $"C-{DateTimeOffset.UtcNow:yyMMdd}-{Random.Shared.Next(10000, 99999)}";
        var customer = Customer.Create(customerNumber, request.FirstName, request.LastName,
            request.FirstNameKa, request.LastNameKa);

        customer.SetContactInfo(request.Phone, request.Email);
        customer.SetCompany(request.CompanyName, request.Tin);
        customer.SetPersonalInfo(request.DateOfBirth, request.Gender);
        customer.SetConsent(request.ConsentSms, request.ConsentEmail);

        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(new CustomerCreatedResponse(customer.Id, customerNumber));
    }
}
