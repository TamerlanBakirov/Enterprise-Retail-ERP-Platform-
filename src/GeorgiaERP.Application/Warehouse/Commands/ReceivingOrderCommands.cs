using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Warehouse;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Warehouse.Commands;

public record CreateReceivingOrderCommand(
    Guid WarehouseId,
    string Source,
    Guid? SourceOrderId,
    Guid? SupplierId,
    DateTimeOffset? ExpectedDate,
    Guid? LocationId,
    string? Notes,
    List<ReceivingLineInput> Lines) : IRequest<Result<ReceivingOrderCreatedResponse>>;

public record ReceivingLineInput(Guid ProductId, decimal ExpectedQty, Guid? VariantId = null);

public record ReceivingOrderCreatedResponse(Guid Id, string ReceivingNumber);

public class CreateReceivingOrderCommandHandler
    : IRequestHandler<CreateReceivingOrderCommand, Result<ReceivingOrderCreatedResponse>>
{
    private readonly IAppDbContext _dbContext;

    public CreateReceivingOrderCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<ReceivingOrderCreatedResponse>> Handle(
        CreateReceivingOrderCommand request, CancellationToken ct)
    {
        var warehouseExists = await _dbContext.Warehouses.AnyAsync(w => w.Id == request.WarehouseId && w.IsActive, ct);
        if (!warehouseExists) return Result.Failure<ReceivingOrderCreatedResponse>("Warehouse not found or inactive.");

        if (!Enum.TryParse<ReceivingOrderSource>(request.Source, true, out var source))
            return Result.Failure<ReceivingOrderCreatedResponse>($"Invalid source '{request.Source}'.");

        var recNumber = $"RCV-{DateTimeOffset.UtcNow:yyMMdd}-{Guid.NewGuid():N}"[..24];
        var order = ReceivingOrder.Create(recNumber, request.WarehouseId, source, request.SourceOrderId, request.SupplierId);

        if (request.ExpectedDate.HasValue) order.SetExpectedDate(request.ExpectedDate);
        if (request.LocationId.HasValue) order.SetLocation(request.LocationId);
        if (request.Notes is not null) order.SetNotes(request.Notes);

        foreach (var input in request.Lines)
        {
            var productExists = await _dbContext.Products.AnyAsync(p => p.Id == input.ProductId && p.IsActive, ct);
            if (!productExists) return Result.Failure<ReceivingOrderCreatedResponse>($"Product {input.ProductId} not found.");

            var line = ReceivingOrderLine.Create(order.Id, input.ProductId, input.ExpectedQty, input.VariantId);
            order.Lines.Add(line);
        }

        _dbContext.ReceivingOrders.Add(order);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(new ReceivingOrderCreatedResponse(order.Id, recNumber));
    }
}

public record StartReceivingCommand(Guid Id) : IRequest<Result>;

public class StartReceivingCommandHandler : IRequestHandler<StartReceivingCommand, Result>
{
    private readonly IAppDbContext _dbContext;

    public StartReceivingCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(StartReceivingCommand request, CancellationToken ct)
    {
        var order = await _dbContext.ReceivingOrders.FirstOrDefaultAsync(o => o.Id == request.Id, ct);
        if (order is null) return Result.NotFound("ReceivingOrder", request.Id);

        try { order.StartReceiving(); }
        catch (InvalidOperationException ex) { return Result.Failure(ex.Message); }

        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public record ReceiveLineCommand(
    Guid OrderId,
    Guid LineId,
    decimal ReceivedQty,
    decimal? DamagedQty,
    string? BatchNumber,
    string? SerialNumber,
    Guid? LocationId) : IRequest<Result>;

public class ReceiveLineCommandHandler : IRequestHandler<ReceiveLineCommand, Result>
{
    private readonly IAppDbContext _dbContext;

    public ReceiveLineCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(ReceiveLineCommand request, CancellationToken ct)
    {
        var order = await _dbContext.ReceivingOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, ct);
        if (order is null) return Result.NotFound("ReceivingOrder", request.OrderId);

        if (order.Status != ReceivingOrderStatus.InProgress)
            return Result.Failure("Order must be InProgress to receive lines.");

        var line = order.Lines.FirstOrDefault(l => l.Id == request.LineId);
        if (line is null) return Result.NotFound("ReceivingOrderLine", request.LineId);

        line.Receive(request.ReceivedQty, request.DamagedQty);
        if (request.BatchNumber is not null)
            line.SetBatch(request.BatchNumber, request.SerialNumber);
        if (request.LocationId.HasValue)
            line.SetLocation(request.LocationId);

        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public record CompleteReceivingCommand(Guid Id, Guid ReceivedBy) : IRequest<Result>;

public class CompleteReceivingCommandHandler : IRequestHandler<CompleteReceivingCommand, Result>
{
    private readonly IAppDbContext _dbContext;

    public CompleteReceivingCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(CompleteReceivingCommand request, CancellationToken ct)
    {
        var order = await _dbContext.ReceivingOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == request.Id, ct);
        if (order is null) return Result.NotFound("ReceivingOrder", request.Id);

        var unreceived = order.Lines.Any(l => l.ReceivedQty == 0);
        if (unreceived) return Result.Failure("All lines must be received before completing.");

        try { order.Complete(request.ReceivedBy); }
        catch (InvalidOperationException ex) { return Result.Failure(ex.Message); }
        catch (ArgumentException ex) { return Result.Failure(ex.Message); }

        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public record CancelReceivingCommand(Guid Id) : IRequest<Result>;

public class CancelReceivingCommandHandler : IRequestHandler<CancelReceivingCommand, Result>
{
    private readonly IAppDbContext _dbContext;

    public CancelReceivingCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(CancelReceivingCommand request, CancellationToken ct)
    {
        var order = await _dbContext.ReceivingOrders.FirstOrDefaultAsync(o => o.Id == request.Id, ct);
        if (order is null) return Result.NotFound("ReceivingOrder", request.Id);

        try { order.Cancel(); }
        catch (InvalidOperationException ex) { return Result.Failure(ex.Message); }
        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}
