using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Organization.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Organization.Queries;

public record GetStoreByIdQuery(Guid Id) : IRequest<Result<StoreDto>>;

public class GetStoreByIdQueryHandler : IRequestHandler<GetStoreByIdQuery, Result<StoreDto>>
{
    private readonly IAppDbContext _dbContext;
    public GetStoreByIdQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<StoreDto>> Handle(GetStoreByIdQuery request, CancellationToken ct)
    {
        var store = await _dbContext.Stores.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.Id, ct);

        return store is null
            ? Result.NotFound<StoreDto>("Store", request.Id)
            : Result.Success(new StoreDto(
                store.Id, store.Code, store.Name, store.NameKa,
                store.StoreType.ToString(), store.Address, store.City, store.Region,
                store.Phone, store.ManagerUserId, store.IsActive, store.CreatedAt));
    }
}
