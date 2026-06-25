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
        // Batch-resolve barcodes to product/variant ids (one query, not one per line).
        var scannedBarcodes = inputs
            .Where(i => !i.ProductId.HasValue && !string.IsNullOrEmpty(i.Barcode))
            .Select(i => i.Barcode!)
            .Distinct()
            .ToList();

        var barcodeMap = scannedBarcodes.Count == 0
            ? new Dictionary<string, (Guid ProductId, Guid? VariantId)>()
            : (await _dbContext.ProductBarcodes.AsNoTracking()
                    .Where(b => scannedBarcodes.Contains(b.Barcode))
                    .Select(b => new { b.Barcode, b.ProductId, b.VariantId })
                    .ToListAsync(ct))
                .GroupBy(b => b.Barcode)
                .ToDictionary(g => g.Key, g => (g.First().ProductId, g.First().VariantId));

        // Resolve each input's (productId, variantId), failing on the first bad line.
        var resolvedKeys = new List<(PosLineInput Input, Guid ProductId, Guid? VariantId)>();
        foreach (var input in inputs)
        {
            if (input.ProductId.HasValue)
            {
                resolvedKeys.Add((input, input.ProductId.Value, null));
            }
            else if (!string.IsNullOrEmpty(input.Barcode))
            {
                if (!barcodeMap.TryGetValue(input.Barcode, out var key))
                    return Result.Failure<List<ResolvedLine>>($"Product not found for barcode '{input.Barcode}'.");
                resolvedKeys.Add((input, key.ProductId, key.VariantId));
            }
            else
            {
                return Result.Failure<List<ResolvedLine>>("Each line must have either ProductId or Barcode.");
            }
        }

        var productIds = resolvedKeys.Select(k => k.ProductId).Distinct().ToList();

        var products = (await _dbContext.Products.AsNoTracking()
                .Where(p => productIds.Contains(p.Id) && p.IsActive)
                .ToListAsync(ct))
            .ToDictionary(p => p.Id);

        // Stock levels for the page's products in this warehouse; matched on variant in memory.
        var stockLevels = await _dbContext.StockLevels
            .Where(s => productIds.Contains(s.ProductId) && s.WarehouseId == warehouseId)
            .ToListAsync(ct);

        // Primary barcodes for lines that were entered by product id (no scanned barcode).
        var needPrimary = resolvedKeys
            .Where(k => string.IsNullOrEmpty(k.Input.Barcode))
            .Select(k => k.ProductId).Distinct().ToList();
        var primaryBarcodes = needPrimary.Count == 0
            ? new Dictionary<Guid, string>()
            : (await _dbContext.ProductBarcodes.AsNoTracking()
                    .Where(b => needPrimary.Contains(b.ProductId) && b.IsPrimary)
                    .Select(b => new { b.ProductId, b.Barcode })
                    .ToListAsync(ct))
                .GroupBy(b => b.ProductId)
                .ToDictionary(g => g.Key, g => g.First().Barcode);

        var result = new List<ResolvedLine>();
        foreach (var (input, productId, variantId) in resolvedKeys)
        {
            if (!products.TryGetValue(productId, out var product))
                return Result.Failure<List<ResolvedLine>>($"Product '{productId}' not found or inactive.");

            var stockLevel = stockLevels.FirstOrDefault(s => s.ProductId == productId && s.VariantId == variantId);
            if (stockLevel is null)
                return Result.Failure<List<ResolvedLine>>($"No stock record for product '{product.Name}' in this store.");

            if (stockLevel.AvailableQuantity < input.Quantity)
                return Result.Failure<List<ResolvedLine>>(
                    $"Insufficient stock for '{product.Name}': available {stockLevel.AvailableQuantity}, requested {input.Quantity}.");

            var barcode = input.Barcode ?? primaryBarcodes.GetValueOrDefault(productId);

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
