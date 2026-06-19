using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Procurement;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Procurement.Commands;

public record CreateSupplierCommand(
    string Code,
    string Name,
    string? NameKa,
    string? Tin,
    bool IsVatPayer,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Address,
    string? PaymentTerms,
    decimal? CreditLimit) : IRequest<Result<Guid>>;

public class CreateSupplierCommandHandler : IRequestHandler<CreateSupplierCommand, Result<Guid>>
{
    private readonly IAppDbContext _dbContext;
    public CreateSupplierCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<Guid>> Handle(CreateSupplierCommand request, CancellationToken ct)
    {
        var exists = await _dbContext.Suppliers.AnyAsync(s => s.Code == request.Code, ct);
        if (exists) return Result.Failure<Guid>($"Supplier code '{request.Code}' already exists.");

        var supplier = Supplier.Create(request.Code, request.Name, request.NameKa, request.Tin);
        supplier.SetVatPayer(request.IsVatPayer);
        supplier.SetContactInfo(request.ContactPerson, request.Phone, request.Email, request.Address);
        supplier.SetPaymentTerms(request.PaymentTerms, request.CreditLimit);

        _dbContext.Suppliers.Add(supplier);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(supplier.Id);
    }
}
