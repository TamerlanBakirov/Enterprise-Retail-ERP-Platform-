using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Organization.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Organization.Queries;

public record GetCompanyQuery : IRequest<CompanyDto?>;

public record GetStoresQuery(bool? IsActive = null) : IRequest<IReadOnlyList<StoreDto>>;

public record GetWarehousesQuery(bool? IsActive = null) : IRequest<IReadOnlyList<WarehouseDto>>;

public class GetCompanyQueryHandler : IRequestHandler<GetCompanyQuery, CompanyDto?>
{
    private readonly IAppDbContext _dbContext;
    public GetCompanyQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<CompanyDto?> Handle(GetCompanyQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.Companies
            .AsNoTracking()
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CompanyDto(
                c.Id, c.Code, c.Name, c.NameKa, c.Tin, c.IsVatPayer,
                c.VatRegistrationDate, c.LegalAddress, c.ActualAddress,
                c.Phone, c.Email, c.IsActive, c.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }
}

public class GetStoresQueryHandler : IRequestHandler<GetStoresQuery, IReadOnlyList<StoreDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetStoresQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<StoreDto>> Handle(GetStoresQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Stores.AsNoTracking();

        if (request.IsActive.HasValue)
            query = query.Where(s => s.IsActive == request.IsActive.Value);

        return await query
            .OrderBy(s => s.Code)
            .Select(s => new StoreDto(
                s.Id, s.Code, s.Name, s.NameKa,
                s.StoreType.ToString(), s.Address, s.City, s.Region,
                s.Phone, s.ManagerUserId, s.IsActive, s.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}

public class GetWarehousesQueryHandler : IRequestHandler<GetWarehousesQuery, IReadOnlyList<WarehouseDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetWarehousesQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<WarehouseDto>> Handle(GetWarehousesQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Warehouses.AsNoTracking();

        if (request.IsActive.HasValue)
            query = query.Where(w => w.IsActive == request.IsActive.Value);

        return await query
            .OrderBy(w => w.Code)
            .Select(w => new WarehouseDto(
                w.Id, w.Code, w.Name, w.NameKa,
                w.WarehouseType.ToString(), w.Address, w.City, w.Region,
                w.LinkedStoreId, w.IsActive, w.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
