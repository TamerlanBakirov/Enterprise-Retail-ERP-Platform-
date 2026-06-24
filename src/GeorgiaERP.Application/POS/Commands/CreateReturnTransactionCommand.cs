using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Inventory;
using GeorgiaERP.Domain.POS;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GeorgiaERP.Application.POS.Commands;

/// <summary>
/// Refunds part or all of a completed sale. Returns the goods to stock and
/// records a Return transaction linked to the original sale. Refund amounts are
/// derived from the original sale lines (not client input) to prevent
/// over-refunding, and a line cannot be returned for more than was sold less
/// what has already been returned across prior returns.
/// </summary>
public record CreateReturnTransactionCommand(
    Guid SessionId,
    Guid OriginalTransactionId,
    List<ReturnLineInput> Lines,
    string? Reason) : IRequest<Result<PosTransactionResponse>>;

public record ReturnLineInput(Guid ProductId, Guid? VariantId, decimal Quantity);

public class CreateReturnTransactionCommandHandler
    : IRequestHandler<CreateReturnTransactionCommand, Result<PosTransactionResponse>>
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<CreateReturnTransactionCommandHandler> _logger;

    public CreateReturnTransactionCommandHandler(
        IAppDbContext dbContext, ILogger<CreateReturnTransactionCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<PosTransactionResponse>> Handle(
        CreateReturnTransactionCommand request, CancellationToken ct)
    {
        if (request.Lines.Count == 0)
            return Result.Failure<PosTransactionResponse>("A return must contain at least one line.");
        if (request.Lines.Any(l => l.Quantity <= 0))
            return Result.Failure<PosTransactionResponse>("Return quantities must be positive.");

        var session = await _dbContext.PosSessions
            .Include(s => s.Terminal)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, ct);
        if (session is null)
            return Result.Failure<PosTransactionResponse>("POS session not found.");
        if (session.Status != PosSessionStatus.Open)
            return Result.Failure<PosTransactionResponse>("POS session is not open.");

        var original = await _dbContext.PosTransactions
            .Include(t => t.Lines)
            .FirstOrDefaultAsync(t => t.Id == request.OriginalTransactionId, ct);
        if (original is null)
            return Result.NotFound<PosTransactionResponse>("PosTransaction", request.OriginalTransactionId);
        if (original.TransactionType != PosTransactionType.Sale || original.Status != PosTransactionStatus.Completed)
            return Result.Failure<PosTransactionResponse>("Only a completed sale can be returned.");

        var storeId = session.Terminal.StoreId;
        var warehouse = await _dbContext.Warehouses
            .FirstOrDefaultAsync(w => w.LinkedStoreId == storeId && w.IsActive, ct);
        if (warehouse is null)
            return Result.Failure<PosTransactionResponse>("No warehouse linked to this store.");

        // Quantities already returned for this sale, per product+variant.
        var priorReturns = await _dbContext.PosTransactions
            .Where(t => t.OriginalTransactionId == original.Id && t.TransactionType == PosTransactionType.Return)
            .Include(t => t.Lines)
            .ToListAsync(ct);
        var alreadyReturned = priorReturns
            .SelectMany(t => t.Lines)
            .GroupBy(l => (l.ProductId, l.VariantId))
            .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));

        var returnNumber = $"RET-{DateTimeOffset.UtcNow:yyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
        var ret = PosTransaction.Create(returnNumber, request.SessionId, storeId, PosTransactionType.Return, Guid.Empty);
        ret.SetOriginalTransaction(original.Id);
        if (original.CustomerId.HasValue)
            ret.SetCustomer(original.CustomerId);

        decimal subtotal = 0, vatTotal = 0, total = 0;
        var lineNumber = 1;
        var stockReturns = new List<StockMovement>();

        foreach (var input in request.Lines)
        {
            var originalLine = original.Lines.FirstOrDefault(l =>
                l.ProductId == input.ProductId && l.VariantId == input.VariantId);
            if (originalLine is null)
                return Result.Failure<PosTransactionResponse>(
                    $"Product '{input.ProductId}' was not part of the original sale.");

            var prior = alreadyReturned.GetValueOrDefault((input.ProductId, input.VariantId), 0m);
            var returnable = originalLine.Quantity - prior;
            if (input.Quantity > returnable)
                return Result.Failure<PosTransactionResponse>(
                    $"Cannot return {input.Quantity} of '{originalLine.ProductName}': only {returnable} remain returnable.");

            // Refund derived from the original line, prorated by returned quantity.
            var perUnitTotal = originalLine.LineTotal / originalLine.Quantity;
            var perUnitVat = originalLine.VatAmount / originalLine.Quantity;
            var lineTotal = Math.Round(perUnitTotal * input.Quantity, 2);
            var lineVat = Math.Round(perUnitVat * input.Quantity, 2);

            var line = PosTransactionLine.Create(
                ret.Id, lineNumber++, input.ProductId,
                originalLine.ProductName, input.Quantity, originalLine.UnitPrice);
            line.SetVariant(input.VariantId);
            line.SetCostPrice(originalLine.CostPrice);
            line.SetVat(lineVat);
            line.SetLineTotal(lineTotal);
            ret.Lines.Add(line);

            subtotal += input.Quantity * originalLine.UnitPrice;
            vatTotal += lineVat;
            total += lineTotal;

            // Goods come back into stock.
            var stock = await _dbContext.StockLevels.FirstOrDefaultAsync(s =>
                s.ProductId == input.ProductId && s.WarehouseId == warehouse.Id &&
                s.VariantId == input.VariantId, ct);
            stock?.AddStock(input.Quantity, MovementType.Return, ret.Id);

            stockReturns.Add(StockMovement.Create(
                MovementType.Return, input.ProductId, warehouse.Id,
                input.Quantity, originalLine.CostPrice, Guid.Empty, input.VariantId));
        }

        ret.SetTotals(subtotal, 0m, vatTotal, total);
        ret.Complete();

        _dbContext.PosTransactions.Add(ret);
        foreach (var movement in stockReturns)
            _dbContext.StockMovements.Add(movement);

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "POS return {RetNumber} for sale {OrigId}: {Total} GEL refunded, {Lines} lines",
            returnNumber, original.Id, total, ret.Lines.Count);

        return Result.Success(new PosTransactionResponse(
            ret.Id, returnNumber, subtotal, 0m, vatTotal, total, null, "Completed"));
    }
}
