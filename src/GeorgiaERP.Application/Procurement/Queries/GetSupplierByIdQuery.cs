using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Procurement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Procurement.Queries;

public record GetSupplierByIdQuery(Guid Id) : IRequest<Result<SupplierDto>>;

public class GetSupplierByIdQueryHandler : IRequestHandler<GetSupplierByIdQuery, Result<SupplierDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetSupplierByIdQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<SupplierDto>> Handle(GetSupplierByIdQuery request, CancellationToken ct)
    {
        var supplier = await _dbContext.Suppliers.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.Id, ct);

        return supplier is null
            ? Result.NotFound<SupplierDto>("Supplier", request.Id)
            : Result.Success(new SupplierDto(
                supplier.Id, supplier.Code, supplier.Name, supplier.NameKa, supplier.Tin,
                supplier.IsVatPayer, supplier.ContactPerson, supplier.Phone, supplier.Email,
                supplier.Address, supplier.PaymentTerms, supplier.CreditLimit, supplier.Rating,
                supplier.IsActive, supplier.CreatedAt));
    }
}
