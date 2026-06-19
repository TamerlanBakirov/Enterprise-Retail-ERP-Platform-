using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Inventory;
using GeorgiaERP.Domain.Procurement;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Procurement.Commands;

public record ReceiveGoodsCommand(
    Guid PurchaseOrderId,
    Guid ReceivedBy,
    string? Notes,
    List<GoodsReceiptLineInput> Lines) : IRequest<Result<GoodsReceiptResponse>>;

public record GoodsReceiptLineInput(
    Guid PoLineId,
    decimal ReceivedQty,
    decimal? AcceptedQty = null,
    decimal? RejectedQty = null,
    string? BatchNumber = null,
    DateTimeOffset? ExpiryDate = null);

public record GoodsReceiptResponse(Guid GrnId, string GrnNumber, int LinesReceived);

public class ReceiveGoodsCommandHandler
    : IRequestHandler<ReceiveGoodsCommand, Result<GoodsReceiptResponse>>
{
    private readonly IAppDbContext _dbContext;

    public ReceiveGoodsCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<GoodsReceiptResponse>> Handle(ReceiveGoodsCommand request, CancellationToken ct)
    {
        var order = await _dbContext.PurchaseOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == request.PurchaseOrderId, ct);

        if (order is null) return Result.Failure<GoodsReceiptResponse>("Purchase order not found.");
        if (order.Status is PurchaseOrderStatus.Draft or PurchaseOrderStatus.Cancelled)
            return Result.Failure<GoodsReceiptResponse>("Order must be approved or sent before receiving.");

        var grnNumber = $"GRN-{DateTimeOffset.UtcNow:yyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
        var grn = GoodsReceiptNote.Create(grnNumber, order.Id, order.WarehouseId, order.SupplierId, request.ReceivedBy);

        if (request.Notes is not null) grn.SetNotes(request.Notes);

        foreach (var input in request.Lines)
        {
            var poLine = order.Lines.FirstOrDefault(l => l.Id == input.PoLineId);
            if (poLine is null)
                return Result.Failure<GoodsReceiptResponse>($"PO line {input.PoLineId} not found.");

            if (input.ReceivedQty > poLine.RemainingQty)
                return Result.Failure<GoodsReceiptResponse>(
                    $"Received qty ({input.ReceivedQty}) exceeds remaining ({poLine.RemainingQty}) for line {poLine.LineNumber}.");

            var grnLine = GoodsReceiptLine.Create(
                grn.Id, poLine.Id, poLine.ProductId, input.ReceivedQty, poLine.UnitPrice, poLine.VariantId);

            if (input.AcceptedQty.HasValue || input.RejectedQty.HasValue)
                grnLine.SetQualityResult(input.AcceptedQty ?? input.ReceivedQty, input.RejectedQty ?? 0);

            if (input.BatchNumber is not null || input.ExpiryDate.HasValue)
                grnLine.SetBatch(input.BatchNumber, input.ExpiryDate);

            grn.Lines.Add(grnLine);
            poLine.AddReceivedQty(input.ReceivedQty);

            var accepted = grnLine.AcceptedQty;
            if (accepted > 0)
            {
                var stock = await _dbContext.StockLevels
                    .FirstOrDefaultAsync(s => s.ProductId == poLine.ProductId
                        && s.WarehouseId == order.WarehouseId
                        && s.VariantId == poLine.VariantId, ct);

                if (stock is null)
                {
                    stock = StockLevel.Create(poLine.ProductId, order.WarehouseId, poLine.UnitPrice, poLine.VariantId);
                    _dbContext.StockLevels.Add(stock);
                }

                stock.AddStock(accepted);

                _dbContext.StockMovements.Add(StockMovement.Create(
                    MovementType.Receipt, poLine.ProductId, order.WarehouseId,
                    accepted, poLine.UnitPrice, request.ReceivedBy, poLine.VariantId));
            }
        }

        grn.Complete();

        var allReceived = order.Lines.All(l => l.RemainingQty <= 0);
        if (allReceived)
            order.MarkReceived();
        else
            order.MarkPartiallyReceived();

        _dbContext.GoodsReceiptNotes.Add(grn);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(new GoodsReceiptResponse(grn.Id, grnNumber, grn.Lines.Count));
    }
}
