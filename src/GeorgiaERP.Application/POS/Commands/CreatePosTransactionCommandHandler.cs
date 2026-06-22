using System.Text.Json;
using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Compliance;
using GeorgiaERP.Domain.Compliance;
using GeorgiaERP.Domain.Inventory;
using GeorgiaERP.Domain.POS;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GeorgiaERP.Application.POS.Commands;

public class CreatePosTransactionCommandHandler
    : IRequestHandler<CreatePosTransactionCommand, Result<PosTransactionResponse>>
{
    private readonly IAppDbContext _dbContext;
    private readonly IRsGeQueuePublisher _queuePublisher;
    private readonly ILogger<CreatePosTransactionCommandHandler> _logger;

    private const decimal VatRate = 0.18m;

    public CreatePosTransactionCommandHandler(
        IAppDbContext dbContext,
        IRsGeQueuePublisher queuePublisher,
        ILogger<CreatePosTransactionCommandHandler> logger)
    {
        _dbContext = dbContext;
        _queuePublisher = queuePublisher;
        _logger = logger;
    }

    public async Task<Result<PosTransactionResponse>> Handle(
        CreatePosTransactionCommand request, CancellationToken cancellationToken)
    {
        var session = await _dbContext.PosSessions
            .Include(s => s.Terminal)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);

        if (session is null)
            return Result.Failure<PosTransactionResponse>("POS session not found.");

        if (session.Status != PosSessionStatus.Open)
            return Result.Failure<PosTransactionResponse>("POS session is not open.");

        var storeId = session.Terminal.StoreId;

        var warehouse = await _dbContext.Warehouses
            .FirstOrDefaultAsync(w => w.LinkedStoreId == storeId && w.IsActive, cancellationToken);

        if (warehouse is null)
            return Result.Failure<PosTransactionResponse>("No warehouse linked to this store.");

        var resolvedLines = await ResolveLines(request.Lines, warehouse.Id, cancellationToken);
        if (resolvedLines.IsFailure)
            return Result.Failure<PosTransactionResponse>(resolvedLines.Error!);

        var transactionNumber = GenerateTransactionNumber(storeId);
        var transaction = PosTransaction.Create(
            transactionNumber, request.SessionId, storeId, PosTransactionType.Sale, Guid.Empty);

        if (request.CustomerId.HasValue)
            transaction.SetCustomer(request.CustomerId);

        decimal subtotal = 0, discountTotal = 0, vatTotal = 0;
        var lineNumber = 1;
        var stockDeductions = new List<(StockLevel Stock, StockMovement Movement)>();

        foreach (var resolved in resolvedLines.Value!)
        {
            var line = PosTransactionLine.Create(
                transaction.Id, lineNumber++, resolved.ProductId,
                resolved.ProductName, resolved.Quantity, resolved.UnitPrice);

            line.SetBarcode(resolved.Barcode);
            line.SetVariant(resolved.VariantId);
            line.SetCostPrice(resolved.CostPrice);
            line.SetDiscount(resolved.DiscountAmount, resolved.DiscountReason);

            var lineSubtotal = resolved.Quantity * resolved.UnitPrice;
            var lineAfterDiscount = lineSubtotal - resolved.DiscountAmount;
            var lineVat = resolved.VatApplicable
                ? Math.Round(lineAfterDiscount * VatRate / (1 + VatRate), 2)
                : 0m;

            line.SetVat(lineVat);
            line.SetLineTotal(lineAfterDiscount);

            subtotal += lineSubtotal;
            discountTotal += resolved.DiscountAmount;
            vatTotal += lineVat;

            transaction.Lines.Add(line);

            resolved.StockLevel.Deduct(resolved.Quantity);

            var movement = StockMovement.Create(
                MovementType.Sale, resolved.ProductId, warehouse.Id,
                -resolved.Quantity, resolved.CostPrice, Guid.Empty,
                resolved.VariantId);

            stockDeductions.Add((resolved.StockLevel, movement));
        }

        var total = subtotal - discountTotal;
        transaction.SetTotals(subtotal, discountTotal, vatTotal, total);

        var paymentTotal = request.Payments.Sum(p => p.Amount);
        if (paymentTotal < total)
            return Result.Failure<PosTransactionResponse>(
                $"Payment total ({paymentTotal:F2} GEL) is less than transaction total ({total:F2} GEL).");

        foreach (var paymentInput in request.Payments)
        {
            var payment = PosPayment.Create(
                transaction.Id, paymentInput.PaymentMethod, paymentInput.Amount);

            if (paymentInput.Reference is not null || paymentInput.TerminalRef is not null)
                payment.SetReference(paymentInput.Reference, paymentInput.TerminalRef);

            if (paymentInput.PaymentMethod == PaymentMethod.Cash && paymentTotal > total)
                payment.SetChange(paymentTotal - total);

            transaction.Payments.Add(payment);
        }

        var fiscalDoc = FiscalDocument.Create(
            FiscalDocumentType.FiscalReceipt,
            internalRef: transactionNumber,
            referenceType: "PosTransaction",
            referenceId: transaction.Id);

        fiscalDoc.MarkQueued();
        fiscalDoc.SetSubmissionDeadline(DateTimeOffset.UtcNow.AddDays(30));

        var receiptData = new
        {
            TransactionNumber = transactionNumber,
            StoreId = storeId,
            Lines = transaction.Lines.Select(l => new
            {
                l.ProductName,
                l.Quantity,
                l.UnitPrice,
                l.VatAmount,
                l.LineTotal
            }),
            Subtotal = subtotal,
            DiscountTotal = discountTotal,
            VatTotal = vatTotal,
            Total = total,
            Payments = request.Payments.Select(p => new { Method = p.PaymentMethod.ToString(), p.Amount })
        };
        fiscalDoc.SetDocumentData(JsonSerializer.Serialize(receiptData));

        transaction.Complete(fiscalDoc.Id.ToString());

        _dbContext.PosTransactions.Add(transaction);
        _dbContext.FiscalDocuments.Add(fiscalDoc);

        foreach (var (_, movement) in stockDeductions)
            _dbContext.StockMovements.Add(movement);

        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await _queuePublisher.PublishAsync(
                new RsGeSubmissionMessage
                {
                    FiscalDocumentId = fiscalDoc.Id,
                    Operation = RsGeOperation.SubmitInvoice
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish fiscal receipt {DocId} to RS.GE queue; recovery sweep will handle it", fiscalDoc.Id);
        }

        _logger.LogInformation(
            "POS sale {TxNumber} completed: {Total} GEL, {LineCount} items, fiscal doc {DocId}",
            transactionNumber, total, transaction.Lines.Count, fiscalDoc.Id);

        return Result.Success(new PosTransactionResponse(
            transaction.Id,
            transactionNumber,
            subtotal,
            discountTotal,
            vatTotal,
            total,
            fiscalDoc.Id,
            "Completed"));
    }

    private async Task<Result<List<ResolvedLine>>> ResolveLines(
        List<PosLineInput> inputs, Guid warehouseId, CancellationToken ct)
    {
        var result = new List<ResolvedLine>();

        foreach (var input in inputs)
        {
            Guid productId;
            Guid? variantId = null;
            string? barcode = input.Barcode;

            if (input.ProductId.HasValue)
            {
                productId = input.ProductId.Value;
            }
            else if (!string.IsNullOrEmpty(input.Barcode))
            {
                var barcodeEntry = await _dbContext.ProductBarcodes
                    .FirstOrDefaultAsync(b => b.Barcode == input.Barcode, ct);

                if (barcodeEntry is null)
                    return Result.Failure<List<ResolvedLine>>($"Product not found for barcode '{input.Barcode}'.");

                productId = barcodeEntry.ProductId;
                variantId = barcodeEntry.VariantId;
            }
            else
            {
                return Result.Failure<List<ResolvedLine>>("Each line must have either ProductId or Barcode.");
            }

            var product = await _dbContext.Products
                .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive, ct);

            if (product is null)
                return Result.Failure<List<ResolvedLine>>($"Product '{productId}' not found or inactive.");

            var stockLevel = await _dbContext.StockLevels
                .FirstOrDefaultAsync(s =>
                    s.ProductId == productId &&
                    s.WarehouseId == warehouseId &&
                    s.VariantId == variantId, ct);

            if (stockLevel is null)
                return Result.Failure<List<ResolvedLine>>($"No stock record for product '{product.Name}' in this store.");

            if (stockLevel.AvailableQuantity < input.Quantity)
                return Result.Failure<List<ResolvedLine>>(
                    $"Insufficient stock for '{product.Name}': available {stockLevel.AvailableQuantity}, requested {input.Quantity}.");

            if (barcode is null)
            {
                var primaryBarcode = await _dbContext.ProductBarcodes
                    .FirstOrDefaultAsync(b => b.ProductId == productId && b.IsPrimary, ct);
                barcode = primaryBarcode?.Barcode;
            }

            result.Add(new ResolvedLine(
                productId, variantId, product.Name, barcode,
                input.Quantity, input.UnitPrice, input.DiscountAmount,
                input.DiscountReason, stockLevel.CostPrice, product.VatApplicable,
                stockLevel));
        }

        return Result.Success(result);
    }

    private static string GenerateTransactionNumber(Guid storeId)
    {
        var storePrefix = storeId.ToString()[..4].ToUpperInvariant();
        var timestamp = DateTimeOffset.UtcNow.ToString("yyMMddHHmmss");
        var seq = Random.Shared.Next(1000, 9999);
        return $"TX-{storePrefix}-{timestamp}-{seq}";
    }

    private record ResolvedLine(
        Guid ProductId,
        Guid? VariantId,
        string ProductName,
        string? Barcode,
        decimal Quantity,
        decimal UnitPrice,
        decimal DiscountAmount,
        string? DiscountReason,
        decimal CostPrice,
        bool VatApplicable,
        StockLevel StockLevel);
}
