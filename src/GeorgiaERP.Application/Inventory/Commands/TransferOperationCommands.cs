using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Inventory.Commands;

public record ApproveTransferCommand(Guid TransferOrderId, Guid ApprovedBy) : IRequest<Result>;
public record ShipTransferCommand(Guid TransferOrderId) : IRequest<Result>;
public record ReceiveTransferCommand(Guid TransferOrderId, List<ReceiveLineInput>? Lines = null) : IRequest<Result>;
public record CancelTransferCommand(Guid TransferOrderId) : IRequest<Result>;

public record ReceiveLineInput(Guid LineId, decimal ReceivedQty);

public class ApproveTransferCommandHandler : IRequestHandler<ApproveTransferCommand, Result>
{
    private readonly IAppDbContext _dbContext;
    public ApproveTransferCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(ApproveTransferCommand request, CancellationToken ct)
    {
        var order = await _dbContext.TransferOrders.FirstOrDefaultAsync(o => o.Id == request.TransferOrderId, ct);
        if (order is null) return Result.Failure("Transfer order not found.");
        if (order.Status != TransferOrderStatus.Draft) return Result.Failure("Only draft orders can be approved.");
        order.Approve(request.ApprovedBy);
        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class ShipTransferCommandHandler : IRequestHandler<ShipTransferCommand, Result>
{
    private readonly IAppDbContext _dbContext;
    public ShipTransferCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(ShipTransferCommand request, CancellationToken ct)
    {
        var order = await _dbContext.TransferOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == request.TransferOrderId, ct);

        if (order is null) return Result.Failure("Transfer order not found.");
        if (order.Status != TransferOrderStatus.Approved) return Result.Failure("Only approved orders can be shipped.");

        foreach (var line in order.Lines)
        {
            var stock = await _dbContext.StockLevels
                .FirstOrDefaultAsync(s => s.ProductId == line.ProductId
                    && s.WarehouseId == order.SourceWarehouseId
                    && s.VariantId == line.VariantId, ct);

            if (stock is null || stock.AvailableQuantity < line.RequestedQty)
                return Result.Failure($"Insufficient stock for product {line.ProductId}.");

            stock.Deduct(line.RequestedQty);
            line.SetShippedQty(line.RequestedQty);

            _dbContext.StockMovements.Add(StockMovement.Create(
                MovementType.TransferOut, line.ProductId, order.SourceWarehouseId,
                -line.RequestedQty, stock.CostPrice, Guid.Empty, line.VariantId));
        }

        order.Ship();
        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class ReceiveTransferCommandHandler : IRequestHandler<ReceiveTransferCommand, Result>
{
    private readonly IAppDbContext _dbContext;
    public ReceiveTransferCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(ReceiveTransferCommand request, CancellationToken ct)
    {
        var order = await _dbContext.TransferOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == request.TransferOrderId, ct);

        if (order is null) return Result.Failure("Transfer order not found.");
        if (order.Status != TransferOrderStatus.InTransit) return Result.Failure("Only in-transit orders can be received.");

        foreach (var line in order.Lines)
        {
            var receivedQty = request.Lines?
                .FirstOrDefault(r => r.LineId == line.Id)?.ReceivedQty ?? line.ShippedQty ?? line.RequestedQty;

            line.SetReceivedQty(receivedQty);

            var sourceStock = await _dbContext.StockLevels
                .FirstOrDefaultAsync(s => s.ProductId == line.ProductId
                    && s.WarehouseId == order.SourceWarehouseId
                    && s.VariantId == line.VariantId, ct);

            var costPrice = sourceStock?.CostPrice ?? 0;

            var destStock = await _dbContext.StockLevels
                .FirstOrDefaultAsync(s => s.ProductId == line.ProductId
                    && s.WarehouseId == order.DestWarehouseId
                    && s.VariantId == line.VariantId, ct);

            if (destStock is null)
            {
                destStock = StockLevel.Create(line.ProductId, order.DestWarehouseId, costPrice, line.VariantId);
                _dbContext.StockLevels.Add(destStock);
            }

            destStock.AddStock(receivedQty);

            _dbContext.StockMovements.Add(StockMovement.Create(
                MovementType.TransferIn, line.ProductId, order.DestWarehouseId,
                receivedQty, costPrice, Guid.Empty, line.VariantId));
        }

        order.Receive();
        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class CancelTransferCommandHandler : IRequestHandler<CancelTransferCommand, Result>
{
    private readonly IAppDbContext _dbContext;
    public CancelTransferCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(CancelTransferCommand request, CancellationToken ct)
    {
        var order = await _dbContext.TransferOrders.FirstOrDefaultAsync(o => o.Id == request.TransferOrderId, ct);
        if (order is null) return Result.Failure("Transfer order not found.");
        if (order.Status is TransferOrderStatus.Received or TransferOrderStatus.Cancelled)
            return Result.Failure("Cannot cancel a received or already cancelled order.");
        order.Cancel();
        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}
