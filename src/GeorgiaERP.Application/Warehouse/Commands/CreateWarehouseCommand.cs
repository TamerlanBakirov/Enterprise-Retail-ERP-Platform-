using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Organization;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Warehouse.Commands;

public record CreateWarehouseCommand(
    string Code,
    string Name,
    string? NameKa,
    string WarehouseType,
    string? Address,
    string? City,
    string? Region,
    Guid? LinkedStoreId) : IRequest<Result<Guid>>;

public class CreateWarehouseCommandHandler : IRequestHandler<CreateWarehouseCommand, Result<Guid>>
{
    private readonly IAppDbContext _dbContext;

    public CreateWarehouseCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<Guid>> Handle(CreateWarehouseCommand request, CancellationToken ct)
    {
        var codeExists = await _dbContext.Warehouses.AnyAsync(w => w.Code == request.Code, ct);
        if (codeExists) return Result.Conflict<Guid>($"Warehouse with code '{request.Code}' already exists.");

        if (!Enum.TryParse<WarehouseType>(request.WarehouseType, true, out var whType))
            return Result.Failure<Guid>($"Invalid warehouse type '{request.WarehouseType}'.");

        if (request.LinkedStoreId.HasValue)
        {
            var storeExists = await _dbContext.Stores.AnyAsync(s => s.Id == request.LinkedStoreId.Value, ct);
            if (!storeExists) return Result.Failure<Guid>("Linked store not found.");
        }

        var warehouse = Domain.Organization.Warehouse.Create(request.Code, request.Name, whType, request.NameKa);

        if (request.Address is not null || request.City is not null || request.Region is not null)
            warehouse.Update(request.Name, request.NameKa, request.Address, request.City, request.Region);

        if (request.LinkedStoreId.HasValue)
            warehouse.LinkToStore(request.LinkedStoreId.Value);

        _dbContext.Warehouses.Add(warehouse);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(warehouse.Id);
    }
}
