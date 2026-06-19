using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GeorgiaERP.Application.Inventory.Commands;

public record CreateTransferOrderCommand(
    Guid SourceWarehouseId,
    Guid DestWarehouseId,
    Guid RequestedBy,
    string? Notes,
    List<TransferLineInput> Lines) : IRequest<Result<TransferOrderResponse>>;

public record TransferLineInput(Guid ProductId, decimal Quantity, Guid? VariantId = null);

public record TransferOrderResponse(Guid Id, string TransferNumber, string Status);

public class CreateTransferOrderCommandHandler
    : IRequestHandler<CreateTransferOrderCommand, Result<TransferOrderResponse>>
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<CreateTransferOrderCommandHandler> _logger;

    public CreateTransferOrderCommandHandler(IAppDbContext dbContext, ILogger<CreateTransferOrderCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<TransferOrderResponse>> Handle(
        CreateTransferOrderCommand request, CancellationToken cancellationToken)
    {
        var sourceExists = await _dbContext.Warehouses.AnyAsync(w => w.Id == request.SourceWarehouseId && w.IsActive, cancellationToken);
        if (!sourceExists) return Result.Failure<TransferOrderResponse>("Source warehouse not found.");

        var destExists = await _dbContext.Warehouses.AnyAsync(w => w.Id == request.DestWarehouseId && w.IsActive, cancellationToken);
        if (!destExists) return Result.Failure<TransferOrderResponse>("Destination warehouse not found.");

        if (request.SourceWarehouseId == request.DestWarehouseId)
            return Result.Failure<TransferOrderResponse>("Source and destination warehouses must be different.");

        var transferNumber = $"TR-{DateTimeOffset.UtcNow:yyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
        var order = TransferOrder.Create(transferNumber, request.SourceWarehouseId, request.DestWarehouseId, request.RequestedBy);

        if (request.Notes is not null)
            order.SetNotes(request.Notes);

        foreach (var input in request.Lines)
        {
            var stockLevel = await _dbContext.StockLevels
                .FirstOrDefaultAsync(s => s.ProductId == input.ProductId
                    && s.WarehouseId == request.SourceWarehouseId
                    && s.VariantId == input.VariantId, cancellationToken);

            if (stockLevel is null || stockLevel.AvailableQuantity < input.Quantity)
                return Result.Failure<TransferOrderResponse>(
                    $"Insufficient stock for product {input.ProductId} in source warehouse.");

            order.Lines.Add(TransferOrderLine.Create(order.Id, input.ProductId, input.Quantity, input.VariantId));
        }

        _dbContext.TransferOrders.Add(order);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Transfer order {Number} created: {Source} → {Dest}, {Count} lines",
            transferNumber, request.SourceWarehouseId, request.DestWarehouseId, request.Lines.Count);

        return Result.Success(new TransferOrderResponse(order.Id, transferNumber, order.Status.ToString()));
    }
}
