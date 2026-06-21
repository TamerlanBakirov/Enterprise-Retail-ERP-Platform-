using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Warehouse;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Warehouse.Commands;

public record CreateWarehouseLocationCommand(
    Guid WarehouseId,
    string Code,
    string Name,
    string? NameKa,
    string LocationType,
    Guid? ParentLocationId,
    int SortOrder,
    int? MaxCapacity,
    string? Notes) : IRequest<Result<Guid>>;

public class CreateWarehouseLocationCommandHandler
    : IRequestHandler<CreateWarehouseLocationCommand, Result<Guid>>
{
    private readonly IAppDbContext _dbContext;

    public CreateWarehouseLocationCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<Guid>> Handle(CreateWarehouseLocationCommand request, CancellationToken ct)
    {
        var warehouseExists = await _dbContext.Warehouses.AnyAsync(w => w.Id == request.WarehouseId && w.IsActive, ct);
        if (!warehouseExists) return Result.Failure<Guid>("Warehouse not found or inactive.");

        if (!Enum.TryParse<LocationType>(request.LocationType, true, out var locType))
            return Result.Failure<Guid>($"Invalid location type '{request.LocationType}'.");

        var codeExists = await _dbContext.WarehouseLocations
            .AnyAsync(l => l.WarehouseId == request.WarehouseId && l.Code == request.Code, ct);
        if (codeExists) return Result.Conflict<Guid>($"Location with code '{request.Code}' already exists in this warehouse.");

        if (request.ParentLocationId.HasValue)
        {
            var parentExists = await _dbContext.WarehouseLocations
                .AnyAsync(l => l.Id == request.ParentLocationId.Value && l.WarehouseId == request.WarehouseId, ct);
            if (!parentExists) return Result.Failure<Guid>("Parent location not found in this warehouse.");
        }

        var location = WarehouseLocation.Create(
            request.WarehouseId, request.Code, request.Name, locType,
            request.ParentLocationId, request.NameKa);

        location.Update(request.Name, request.NameKa, request.SortOrder, request.MaxCapacity, request.Notes);

        _dbContext.WarehouseLocations.Add(location);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(location.Id);
    }
}

public record UpdateWarehouseLocationCommand(
    Guid Id,
    string Name,
    string? NameKa,
    int SortOrder,
    int? MaxCapacity,
    string? Notes) : IRequest<Result>;

public class UpdateWarehouseLocationCommandHandler
    : IRequestHandler<UpdateWarehouseLocationCommand, Result>
{
    private readonly IAppDbContext _dbContext;

    public UpdateWarehouseLocationCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(UpdateWarehouseLocationCommand request, CancellationToken ct)
    {
        var location = await _dbContext.WarehouseLocations.FirstOrDefaultAsync(l => l.Id == request.Id, ct);
        if (location is null) return Result.NotFound("WarehouseLocation", request.Id);

        location.Update(request.Name, request.NameKa, request.SortOrder, request.MaxCapacity, request.Notes);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }
}

public record DeactivateWarehouseLocationCommand(Guid Id) : IRequest<Result>;

public class DeactivateWarehouseLocationCommandHandler
    : IRequestHandler<DeactivateWarehouseLocationCommand, Result>
{
    private readonly IAppDbContext _dbContext;

    public DeactivateWarehouseLocationCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(DeactivateWarehouseLocationCommand request, CancellationToken ct)
    {
        var location = await _dbContext.WarehouseLocations.FirstOrDefaultAsync(l => l.Id == request.Id, ct);
        if (location is null) return Result.NotFound("WarehouseLocation", request.Id);

        location.Deactivate();
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }
}
