using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Compliance;
using GeorgiaERP.Domain.Inventory;
using GeorgiaERP.Domain.POS;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.POS.Commands;

public record VoidPosTransactionCommand(
    Guid TransactionId,
    Guid VoidedBy,
    string Reason) : IRequest<Result>;

public class VoidPosTransactionCommandHandler : IRequestHandler<VoidPosTransactionCommand, Result>
{
    private readonly IAppDbContext _dbContext;

    public VoidPosTransactionCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> Handle(VoidPosTransactionCommand request, CancellationToken cancellationToken)
    {
        var transaction = await _dbContext.PosTransactions
            .Include(t => t.Lines)
            .Include(t => t.Session)
                .ThenInclude(s => s.Terminal)
            .FirstOrDefaultAsync(t => t.Id == request.TransactionId, cancellationToken);

        if (transaction is null)
            return Result.Failure("Transaction not found.");

        if (transaction.Status == PosTransactionStatus.Voided)
            return Result.Failure("Transaction is already voided.");

        if (transaction.Status != PosTransactionStatus.Completed)
            return Result.Failure("Only completed transactions can be voided.");

        var storeId = transaction.Session.Terminal.StoreId;
        var warehouse = await _dbContext.Warehouses
            .FirstOrDefaultAsync(w => w.LinkedStoreId == storeId && w.IsActive, cancellationToken);

        if (warehouse is not null)
        {
            foreach (var line in transaction.Lines)
            {
                var stockLevel = await _dbContext.StockLevels
                    .FirstOrDefaultAsync(s =>
                        s.ProductId == line.ProductId &&
                        s.WarehouseId == warehouse.Id &&
                        s.VariantId == line.VariantId, cancellationToken);

                if (stockLevel is not null)
                {
                    stockLevel.AddStock(line.Quantity);

                    var movement = StockMovement.Create(
                        MovementType.Return, line.ProductId, warehouse.Id,
                        line.Quantity, line.CostPrice, request.VoidedBy,
                        line.VariantId);

                    _dbContext.StockMovements.Add(movement);
                }
            }
        }

        transaction.Void(request.VoidedBy, request.Reason);

        if (transaction.FiscalReceiptId is not null &&
            Guid.TryParse(transaction.FiscalReceiptId, out var fiscalDocId))
        {
            var fiscalDoc = await _dbContext.FiscalDocuments
                .FirstOrDefaultAsync(d => d.Id == fiscalDocId, cancellationToken);

            if (fiscalDoc is not null &&
                fiscalDoc.Status is FiscalDocumentStatus.Queued or FiscalDocumentStatus.Pending)
            {
                fiscalDoc.MarkFailed($"Voided: {request.Reason}");
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
