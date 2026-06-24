using System.Diagnostics;
using System.Text.Json;
using GeorgiaERP.Application.Compliance;
using GeorgiaERP.Domain.Compliance;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GeorgiaERP.Infrastructure.RsGe;

/// <summary>
/// Executes one RS.GE submission for a fiscal document. Rehydrates the waybill
/// from the database, calls the Revenue Service, records request/response audit
/// logs, and transitions both the fiscal document and waybill state machines.
/// Failure classification drives the consumer's retry-vs-dead-letter decision:
/// network/availability problems are transient; RS.GE business rejections are
/// permanent and require human correction.
/// </summary>
public class RsGeSubmissionProcessor : IRsGeSubmissionProcessor
{
    private readonly AppDbContext _dbContext;
    private readonly IRsGeSoapClient _soapClient;
    private readonly ILogger<RsGeSubmissionProcessor> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public RsGeSubmissionProcessor(
        AppDbContext dbContext,
        IRsGeSoapClient soapClient,
        ILogger<RsGeSubmissionProcessor> logger)
    {
        _dbContext = dbContext;
        _soapClient = soapClient;
        _logger = logger;
    }

    public async Task<RsGeSubmissionResult> ProcessAsync(RsGeSubmissionMessage message, CancellationToken cancellationToken = default)
    {
        var document = await _dbContext.FiscalDocuments
            .FirstOrDefaultAsync(d => d.Id == message.FiscalDocumentId, cancellationToken);

        if (document is null)
        {
            // The document vanished — nothing actionable, so acknowledge to drain the queue.
            _logger.LogWarning("Fiscal document {DocumentId} not found; dropping message", message.FiscalDocumentId);
            return RsGeSubmissionResult.Success("Document not found");
        }

        if (document.Status is FiscalDocumentStatus.Confirmed or FiscalDocumentStatus.Cancelled)
        {
            // Idempotency guard: already in a terminal state, do not resubmit.
            return RsGeSubmissionResult.Success($"Already {document.Status}");
        }

        return message.Operation switch
        {
            RsGeOperation.SubmitWaybill => await SubmitWaybillAsync(document, cancellationToken),
            RsGeOperation.ConfirmWaybill => await ConfirmWaybillAsync(document, cancellationToken),
            RsGeOperation.CloseWaybill => await CloseWaybillAsync(document, cancellationToken),
            RsGeOperation.SubmitInvoice => await SubmitInvoiceAsync(document, cancellationToken),
            RsGeOperation.SubmitVatDeclaration => await SubmitVatDeclarationAsync(document, cancellationToken),
            _ => RsGeSubmissionResult.Permanent($"Unsupported operation {message.Operation}")
        };
    }

    private async Task<RsGeSubmissionResult> SubmitVatDeclarationAsync(FiscalDocument document, CancellationToken cancellationToken)
    {
        if (document.DocumentType is not FiscalDocumentType.VatDeclaration)
            return await FailPermanentAsync(document, "Document is not a VAT declaration", cancellationToken);

        if (document.ReferenceId is not { } declarationId)
            return await FailPermanentAsync(document, "VAT fiscal document is not linked to a declaration", cancellationToken);

        var declaration = await _dbContext.VatDeclarations
            .FirstOrDefaultAsync(v => v.Id == declarationId, cancellationToken);
        if (declaration is null)
            return await FailPermanentAsync(document, $"VAT declaration {declarationId} not found", cancellationToken);

        var request = new RsGeVatDeclarationRequest
        {
            PeriodStart = declaration.PeriodStart,
            PeriodEnd = declaration.PeriodEnd,
            TotalOutputVat = declaration.TotalOutputVat,
            TotalInputVat = declaration.TotalInputVat,
            NetVat = declaration.NetVat
        };

        var log = StartLog(document.Id, "save_vat_declaration", SerializeForAudit(request));
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await _soapClient.SubmitVatDeclarationAsync(request);
            stopwatch.Stop();
            CompleteLog(log, SerializeForAudit(result), 200, (int)stopwatch.ElapsedMilliseconds,
                result.Success ? null : $"{result.ErrorCode}: {result.ErrorMessage}");

            if (!result.Success)
            {
                // RS.GE rejected the return content — requires correction, not retry.
                // FailPermanentAsync persists both the document and declaration changes.
                declaration.MarkRejected();
                return await FailPermanentAsync(document, result.ErrorMessage, cancellationToken);
            }

            document.MarkSubmitted(result.ErrorCode, document.InternalRef);
            document.MarkConfirmed("ACCEPTED");
            declaration.MarkAccepted();
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("VAT declaration {DeclarationId} accepted by RS.GE", declaration.Id);
            return RsGeSubmissionResult.Success();
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            CompleteLog(log, null, 0, (int)stopwatch.ElapsedMilliseconds, ex.Message);
            return await FailTransientAsync(document, ex.Message, cancellationToken);
        }
    }

    private async Task<RsGeSubmissionResult> SubmitInvoiceAsync(FiscalDocument document, CancellationToken cancellationToken)
    {
        if (document.DocumentType is not (FiscalDocumentType.Invoice or FiscalDocumentType.FiscalReceipt))
            return await FailPermanentAsync(document, "Document is not an invoice or fiscal receipt", cancellationToken);

        InvoiceDocumentData? data;
        try
        {
            data = JsonSerializer.Deserialize<InvoiceDocumentData>(document.DocumentData ?? "", JsonOptions);
        }
        catch (JsonException ex)
        {
            return await FailPermanentAsync(document, $"Invalid invoice document data: {ex.Message}", cancellationToken);
        }

        if (data is null || data.Lines.Count == 0)
            return await FailPermanentAsync(document, "Invoice has no lines", cancellationToken);

        var request = new RsGeInvoiceRequest
        {
            BuyerTin = data.BuyerTin ?? "",
            BuyerName = data.BuyerName ?? "Retail customer",
            InvoiceDate = data.InvoiceDate ?? document.CreatedAt,
            Items = data.Lines.Select(line => new RsGeInvoiceItem(
                line.ProductName, line.Quantity, line.UnitPrice, line.VatAmount)).ToList()
        };

        var log = StartLog(document.Id, "save_invoice", SerializeForAudit(request));
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await _soapClient.SaveInvoiceAsync(request);
            stopwatch.Stop();
            CompleteLog(log, SerializeForAudit(result), 200, (int)stopwatch.ElapsedMilliseconds,
                result.Success ? null : $"{result.ErrorCode}: {result.ErrorMessage}");
            if (!result.Success)
                return await FailPermanentAsync(document, result.ErrorMessage, cancellationToken);

            document.MarkSubmitted(result.ErrorCode, document.InternalRef);
            document.MarkConfirmed("SUBMITTED");
            await _dbContext.SaveChangesAsync(cancellationToken);
            return RsGeSubmissionResult.Success(document.InternalRef);
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            CompleteLog(log, null, 0, (int)stopwatch.ElapsedMilliseconds, ex.Message);
            return await FailTransientAsync(document, ex.Message, cancellationToken);
        }
    }

    private async Task<RsGeSubmissionResult> SubmitWaybillAsync(FiscalDocument document, CancellationToken cancellationToken)
    {
        var waybill = await _dbContext.RsGeWaybills
            .FirstOrDefaultAsync(w => w.FiscalDocumentId == document.Id, cancellationToken);

        if (waybill is null)
            return await FailPermanentAsync(document, "No RsGeWaybill associated with fiscal document", cancellationToken);

        var goods = DeserializeGoods(waybill.GoodsData);
        if (goods.Count == 0)
            return await FailPermanentAsync(document, "Waybill has no goods lines", cancellationToken);

        var request = new RsGeWaybillRequest
        {
            WaybillType = int.TryParse(waybill.WaybillType, out var wt) ? wt : 2,
            BuyerTin = waybill.BuyerTin ?? "",
            StartAddress = waybill.StartAddress ?? "",
            EndAddress = waybill.EndAddress ?? "",
            CarNumber = waybill.VehicleNumber,
            DriverTin = waybill.DriverTin,
            Goods = goods.Select(g => new RsGeGoodsItem(g.ProductName, g.UnitId, g.Quantity, g.Price, g.BarCode)).ToList()
        };

        var log = StartLog(document.Id, "save_waybill", SerializeForAudit(request));
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await _soapClient.SaveWaybillAsync(request);
            stopwatch.Stop();
            CompleteLog(log, SerializeForAudit(result), 200, (int)stopwatch.ElapsedMilliseconds,
                result.Success ? null : $"{result.ErrorCode}: {result.ErrorMessage}");

            if (!result.Success)
            {
                // RS.GE rejected the content — a retry would fail identically.
                document.MarkRejected($"save_waybill rejected ({result.ErrorCode}): {result.ErrorMessage}");
                waybill.MarkRejected();
                await _dbContext.SaveChangesAsync(cancellationToken);
                return RsGeSubmissionResult.Permanent(result.ErrorMessage);
            }

            waybill.MarkSaved(result.WaybillNumber);
            document.MarkSubmitted(result.WaybillId?.ToString(), result.WaybillNumber);

            // Activate the waybill so goods may legally move.
            if (result.WaybillId is { } waybillId)
            {
                var sendLog = StartLog(document.Id, "send_waybill", waybillId.ToString());
                var sendWatch = Stopwatch.StartNew();
                var sendResult = await _soapClient.SendWaybillAsync(waybillId);
                sendWatch.Stop();
                CompleteLog(sendLog, SerializeForAudit(sendResult), 200, (int)sendWatch.ElapsedMilliseconds,
                    sendResult.Success ? null : $"{sendResult.ErrorCode}: {sendResult.ErrorMessage}");

                if (sendResult.Success)
                    waybill.MarkActive(DateTimeOffset.UtcNow);
                else
                    _logger.LogWarning("save_waybill succeeded but send_waybill failed for document {DocumentId}: {Error}",
                        document.Id, sendResult.ErrorMessage);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Waybill submitted for document {DocumentId}, RS.GE id {RsGeId}", document.Id, result.WaybillId);
            return RsGeSubmissionResult.Success(result.WaybillNumber);
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            CompleteLog(log, null, 0, (int)stopwatch.ElapsedMilliseconds, ex.Message);
            return await FailTransientAsync(document, ex.Message, cancellationToken);
        }
    }

    private async Task<RsGeSubmissionResult> ConfirmWaybillAsync(FiscalDocument document, CancellationToken cancellationToken)
    {
        if (!int.TryParse(document.RsGeId, out var waybillId))
            return await FailPermanentAsync(document, "Cannot confirm: missing RS.GE waybill id", cancellationToken);

        var waybill = await _dbContext.RsGeWaybills.FirstOrDefaultAsync(w => w.FiscalDocumentId == document.Id, cancellationToken);
        var log = StartLog(document.Id, "confirm_waybill", waybillId.ToString());
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await _soapClient.ConfirmWaybillAsync(waybillId);
            stopwatch.Stop();
            CompleteLog(log, SerializeForAudit(result), 200, (int)stopwatch.ElapsedMilliseconds,
                result.Success ? null : $"{result.ErrorCode}: {result.ErrorMessage}");

            if (!result.Success)
                return await FailPermanentAsync(document, result.ErrorMessage, cancellationToken);

            document.MarkConfirmed("CONFIRMED");
            waybill?.MarkConfirmed();
            await _dbContext.SaveChangesAsync(cancellationToken);
            return RsGeSubmissionResult.Success();
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            CompleteLog(log, null, 0, (int)stopwatch.ElapsedMilliseconds, ex.Message);
            return await FailTransientAsync(document, ex.Message, cancellationToken);
        }
    }

    private async Task<RsGeSubmissionResult> CloseWaybillAsync(FiscalDocument document, CancellationToken cancellationToken)
    {
        if (!int.TryParse(document.RsGeId, out var waybillId))
            return await FailPermanentAsync(document, "Cannot close: missing RS.GE waybill id", cancellationToken);

        var waybill = await _dbContext.RsGeWaybills.FirstOrDefaultAsync(w => w.FiscalDocumentId == document.Id, cancellationToken);
        var log = StartLog(document.Id, "close_waybill", waybillId.ToString());
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await _soapClient.CloseWaybillAsync(waybillId);
            stopwatch.Stop();
            CompleteLog(log, SerializeForAudit(result), 200, (int)stopwatch.ElapsedMilliseconds,
                result.Success ? null : $"{result.ErrorCode}: {result.ErrorMessage}");

            if (!result.Success)
                return await FailPermanentAsync(document, result.ErrorMessage, cancellationToken);

            waybill?.MarkClosed(DateTimeOffset.UtcNow);
            document.MarkConfirmed("CLOSED");
            await _dbContext.SaveChangesAsync(cancellationToken);
            return RsGeSubmissionResult.Success();
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            CompleteLog(log, null, 0, (int)stopwatch.ElapsedMilliseconds, ex.Message);
            return await FailTransientAsync(document, ex.Message, cancellationToken);
        }
    }

    private async Task<RsGeSubmissionResult> FailTransientAsync(FiscalDocument document, string? error, CancellationToken cancellationToken)
    {
        document.MarkFailed(error);
        document.IncrementRetry();
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogWarning("Transient RS.GE failure for document {DocumentId} (retry {Retry}): {Error}",
            document.Id, document.RetryCount, error);
        return RsGeSubmissionResult.Transient(error);
    }

    private async Task<RsGeSubmissionResult> FailPermanentAsync(FiscalDocument document, string? error, CancellationToken cancellationToken)
    {
        document.MarkRejected(error);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogError("Permanent RS.GE failure for document {DocumentId}: {Error}", document.Id, error);
        return RsGeSubmissionResult.Permanent(error);
    }

    private RsGeCommunicationLog StartLog(Guid documentId, string operation, string? requestPayload)
    {
        var log = RsGeCommunicationLog.Create(documentId, operation, CommunicationDirection.Request,
            "https://services.rs.ge/WayBillService/WayBillService.asmx");
        log.SetRequest(requestPayload, Guid.NewGuid());
        _dbContext.RsGeCommunicationLogs.Add(log);
        return log;
    }

    private static void CompleteLog(RsGeCommunicationLog log, string? responsePayload, int httpStatus, int durationMs, string? error)
    {
        log.SetResponse(responsePayload, httpStatus, durationMs, error);
    }

    private static List<WaybillGoodsItem> DeserializeGoods(string? goodsJson)
    {
        if (string.IsNullOrWhiteSpace(goodsJson))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<WaybillGoodsItem>>(goodsJson, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string SerializeForAudit(object value) => JsonSerializer.Serialize(value, JsonOptions);

    private sealed record InvoiceDocumentData
    {
        public string? BuyerTin { get; init; }
        public string? BuyerName { get; init; }
        public DateTimeOffset? InvoiceDate { get; init; }
        public List<InvoiceDocumentLine> Lines { get; init; } = [];
    }

    private sealed record InvoiceDocumentLine(string ProductName, decimal Quantity, decimal UnitPrice, decimal VatAmount);
}
