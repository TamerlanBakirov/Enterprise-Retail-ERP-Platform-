using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Procurement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Procurement.Commands;

/// <summary>
/// Updates an existing supplier's mutable details. The supplier Code is its
/// stable identifier and is not changed here. An optional Rating (1-5) updates
/// the supplier scorecard when supplied.
/// </summary>
public record UpdateSupplierCommand(
    Guid Id,
    string Name,
    string? NameKa,
    string? Tin,
    bool IsVatPayer,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Address,
    string? PaymentTerms,
    decimal? CreditLimit,
    int? Rating,
    bool IsActive) : IRequest<Result<SupplierDto>>;

public class UpdateSupplierCommandHandler : IRequestHandler<UpdateSupplierCommand, Result<SupplierDto>>
{
    private readonly IAppDbContext _dbContext;
    public UpdateSupplierCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<SupplierDto>> Handle(UpdateSupplierCommand request, CancellationToken ct)
    {
        var supplier = await _dbContext.Suppliers.FirstOrDefaultAsync(s => s.Id == request.Id, ct);
        if (supplier is null)
            return Result.NotFound<SupplierDto>("Supplier", request.Id);

        if (request.Rating is { } rating && rating is < 1 or > 5)
            return Result.Failure<SupplierDto>("Rating must be between 1 and 5.");

        supplier.UpdateDetails(request.Name, request.NameKa, request.Tin);
        supplier.SetVatPayer(request.IsVatPayer);
        supplier.SetContactInfo(request.ContactPerson, request.Phone, request.Email, request.Address);
        supplier.SetPaymentTerms(request.PaymentTerms, request.CreditLimit);
        if (request.Rating is { } r)
            supplier.SetRating(r);
        if (request.IsActive && !supplier.IsActive) supplier.Activate();
        else if (!request.IsActive && supplier.IsActive) supplier.Deactivate();

        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(new SupplierDto(
            supplier.Id, supplier.Code, supplier.Name, supplier.NameKa, supplier.Tin,
            supplier.IsVatPayer, supplier.ContactPerson, supplier.Phone, supplier.Email,
            supplier.Address, supplier.PaymentTerms, supplier.CreditLimit, supplier.Rating,
            supplier.IsActive, supplier.CreatedAt));
    }
}
