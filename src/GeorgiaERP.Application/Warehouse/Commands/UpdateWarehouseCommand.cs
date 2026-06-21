using GeorgiaERP.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Warehouse.Commands;

public record UpdateWarehouseCommand(
    Guid Id,
    string Name,
    string? NameKa,
    string? Address,
    string? City,
    string? Region,
    Guid? LinkedStoreId) : IRequest<Result>;

public class UpdateWarehouseCommandHandler : IRequestHandler<UpdateWarehouseCommand, Result>
{
    private readonly IAppDbContext _dbContext;

    public UpdateWarehouseCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(UpdateWarehouseCommand request, CancellationToken ct)
    {
        var warehouse = await _dbContext.Warehouses.FirstOrDefaultAsync(w => w.Id == request.Id, ct);
        if (warehouse is null) return Result.NotFound("Warehouse", request.Id);

        if (request.LinkedStoreId.HasValue)
        {
            var storeExists = await _dbContext.Stores.AnyAsync(s => s.Id == request.LinkedStoreId.Value, ct);
            if (!storeExists) return Result.Failure("Linked store not found.");
        }

        warehouse.Update(request.Name, request.NameKa, request.Address, request.City, request.Region);

        if (request.LinkedStoreId.HasValue)
            warehouse.LinkToStore(request.LinkedStoreId.Value);

        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public record ActivateWarehouseCommand(Guid Id) : IRequest<Result>;

public class ActivateWarehouseCommandHandler : IRequestHandler<ActivateWarehouseCommand, Result>
{
    private readonly IAppDbContext _dbContext;

    public ActivateWarehouseCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(ActivateWarehouseCommand request, CancellationToken ct)
    {
        var warehouse = await _dbContext.Warehouses.FirstOrDefaultAsync(w => w.Id == request.Id, ct);
        if (warehouse is null) return Result.NotFound("Warehouse", request.Id);

        warehouse.Activate();
        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public record DeactivateWarehouseCommand(Guid Id) : IRequest<Result>;

public class DeactivateWarehouseCommandHandler : IRequestHandler<DeactivateWarehouseCommand, Result>
{
    private readonly IAppDbContext _dbContext;

    public DeactivateWarehouseCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(DeactivateWarehouseCommand request, CancellationToken ct)
    {
        var warehouse = await _dbContext.Warehouses.FirstOrDefaultAsync(w => w.Id == request.Id, ct);
        if (warehouse is null) return Result.NotFound("Warehouse", request.Id);

        warehouse.Deactivate();
        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}
