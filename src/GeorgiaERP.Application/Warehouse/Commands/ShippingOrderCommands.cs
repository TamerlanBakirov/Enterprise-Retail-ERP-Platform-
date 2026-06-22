using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Warehouse;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Warehouse.Commands;

public record CreateShippingOrderCommand(
    Guid WarehouseId,
    string OrderType,
    Guid? SourceOrderId,
    Guid? CustomerId,
    Guid? DestWarehouseId,
    string? ShippingAddress,
    string? Carrier,
    DateTimeOffset? ExpectedShipDate,
    string? Notes,
    List<ShippingLineInput> Lines) : IRequest<Result<ShippingOrderCreatedResponse>>;

public record ShippingLineInput(Guid ProductId, decimal OrderedQty, Guid? VariantId = null);

public record ShippingOrderCreatedResponse(Guid Id, string ShippingNumber);

public class CreateShippingOrderCommandHandler
    : IRequestHandler<CreateShippingOrderCommand, Result<ShippingOrderCreatedResponse>>
{
    private readonly IAppDbContext _dbContext;

    public CreateShippingOrderCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<ShippingOrderCreatedResponse>> Handle(
        CreateShippingOrderCommand request, CancellationToken ct)
    {
        var warehouseExists = await _dbContext.Warehouses.AnyAsync(w => w.Id == request.WarehouseId && w.IsActive, ct);
        if (!warehouseExists) return Result.Failure<ShippingOrderCreatedResponse>("Warehouse not found or inactive.");

        if (!Enum.TryParse<ShippingOrderType>(request.OrderType, true, out var orderType))
            return Result.Failure<ShippingOrderCreatedResponse>($"Invalid order type '{request.OrderType}'.");

        var shipNumber = $"SHP-{DateTimeOffset.UtcNow:yyMMdd}-{Guid.NewGuid():N}"[..24];
        var order = ShippingOrder.Create(shipNumber, request.WarehouseId, orderType, request.SourceOrderId, request.CustomerId);

        order.SetShippingDetails(request.ShippingAddress, request.Carrier, request.ExpectedShipDate);
        if (request.DestWarehouseId.HasValue) order.SetDestWarehouse(request.DestWarehouseId);
        if (request.Notes is not null) order.SetNotes(request.Notes);

        foreach (var input in request.Lines)
        {
            var productExists = await _dbContext.Products.AnyAsync(p => p.Id == input.ProductId && p.IsActive, ct);
            if (!productExists) return Result.Failure<ShippingOrderCreatedResponse>($"Product {input.ProductId} not found.");

            var line = ShippingOrderLine.Create(order.Id, input.ProductId, input.OrderedQty, input.VariantId);
            order.Lines.Add(line);
        }

        _dbContext.ShippingOrders.Add(order);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(new ShippingOrderCreatedResponse(order.Id, shipNumber));
    }
}

public record StartPickingCommand(Guid Id) : IRequest<Result>;

public class StartPickingCommandHandler : IRequestHandler<StartPickingCommand, Result>
{
    private readonly IAppDbContext _dbContext;

    public StartPickingCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(StartPickingCommand request, CancellationToken ct)
    {
        var order = await _dbContext.ShippingOrders.FirstOrDefaultAsync(o => o.Id == request.Id, ct);
        if (order is null) return Result.NotFound("ShippingOrder", request.Id);

        try { order.StartPicking(); }
        catch (InvalidOperationException ex) { return Result.Failure(ex.Message); }

        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public record PickLineCommand(
    Guid OrderId,
    Guid LineId,
    decimal PickedQty,
    Guid? LocationId) : IRequest<Result>;

public class PickLineCommandHandler : IRequestHandler<PickLineCommand, Result>
{
    private readonly IAppDbContext _dbContext;

    public PickLineCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(PickLineCommand request, CancellationToken ct)
    {
        var order = await _dbContext.ShippingOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, ct);
        if (order is null) return Result.NotFound("ShippingOrder", request.OrderId);

        if (order.Status != ShippingOrderStatus.Picking)
            return Result.Failure("Order must be in Picking status.");

        var line = order.Lines.FirstOrDefault(l => l.Id == request.LineId);
        if (line is null) return Result.NotFound("ShippingOrderLine", request.LineId);

        line.Pick(request.PickedQty, request.LocationId);
        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public record PackShippingOrderCommand(Guid Id) : IRequest<Result>;

public class PackShippingOrderCommandHandler : IRequestHandler<PackShippingOrderCommand, Result>
{
    private readonly IAppDbContext _dbContext;

    public PackShippingOrderCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(PackShippingOrderCommand request, CancellationToken ct)
    {
        var order = await _dbContext.ShippingOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == request.Id, ct);
        if (order is null) return Result.NotFound("ShippingOrder", request.Id);

        var unpicked = order.Lines.Any(l => l.PickedQty == 0);
        if (unpicked) return Result.Failure("All lines must be picked before packing.");

        foreach (var line in order.Lines)
            line.Pack(line.PickedQty);

        try { order.MarkPacked(); }
        catch (InvalidOperationException ex) { return Result.Failure(ex.Message); }

        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public record ShipOrderCommand(Guid Id, Guid ShippedBy, string? TrackingNumber) : IRequest<Result>;

public class ShipOrderCommandHandler : IRequestHandler<ShipOrderCommand, Result>
{
    private readonly IAppDbContext _dbContext;

    public ShipOrderCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(ShipOrderCommand request, CancellationToken ct)
    {
        var order = await _dbContext.ShippingOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == request.Id, ct);
        if (order is null) return Result.NotFound("ShippingOrder", request.Id);

        foreach (var line in order.Lines)
            line.SetShippedQty(line.PackedQty);

        try { order.Ship(request.ShippedBy, request.TrackingNumber); }
        catch (InvalidOperationException ex) { return Result.Failure(ex.Message); }
        catch (ArgumentException ex) { return Result.Failure(ex.Message); }

        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public record CancelShippingOrderCommand(Guid Id) : IRequest<Result>;

public class CancelShippingOrderCommandHandler : IRequestHandler<CancelShippingOrderCommand, Result>
{
    private readonly IAppDbContext _dbContext;

    public CancelShippingOrderCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(CancelShippingOrderCommand request, CancellationToken ct)
    {
        var order = await _dbContext.ShippingOrders.FirstOrDefaultAsync(o => o.Id == request.Id, ct);
        if (order is null) return Result.NotFound("ShippingOrder", request.Id);

        try { order.Cancel(); }
        catch (InvalidOperationException ex) { return Result.Failure(ex.Message); }
        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}
