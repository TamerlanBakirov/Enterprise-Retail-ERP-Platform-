using System.Text.Json;
using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Compliance;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GeorgiaERP.Application.Compliance.Commands;

public class CreateWaybillCommandHandler : IRequestHandler<CreateWaybillCommand, Result<CreateWaybillResponse>>
{
    private readonly IAppDbContext _dbContext;
    private readonly IRsGeQueuePublisher _publisher;
    private readonly ILogger<CreateWaybillCommandHandler> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public CreateWaybillCommandHandler(
        IAppDbContext dbContext,
        IRsGeQueuePublisher publisher,
        ILogger<CreateWaybillCommandHandler> logger)
    {
        _dbContext = dbContext;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Result<CreateWaybillResponse>> Handle(CreateWaybillCommand request, CancellationToken cancellationToken)
    {
        if (request.Goods.Count == 0)
            return Result.Failure<CreateWaybillResponse>("A waybill must contain at least one goods line.");

        var document = FiscalDocument.Create(
            FiscalDocumentType.Waybill,
            internalRef: request.InternalRef,
            referenceType: request.ReferenceType,
            referenceId: request.ReferenceId);

        // RS.GE requires waybills to be uploaded within 30 days; track the deadline
        // so monitoring can surface documents at risk of the 100% VAT penalty.
        document.SetSubmissionDeadline(DateTimeOffset.UtcNow.AddDays(30));

        var waybill = RsGeWaybill.Create(document.Id, request.WaybillType.ToString());
        waybill.SetParties(request.SellerTin, request.SellerName, request.BuyerTin, request.BuyerName);
        waybill.SetTransport(
            transporterTin: null,
            transportType: request.TransportType,
            vehicleNumber: request.VehicleNumber,
            driverTin: request.DriverTin,
            startAddress: request.StartAddress,
            endAddress: request.EndAddress);

        var goodsJson = JsonSerializer.Serialize(request.Goods, JsonOptions);
        var totalAmount = request.Goods.Sum(g => g.Quantity * g.Price);
        waybill.SetGoods(goodsJson, totalAmount);

        document.SetDocumentData(goodsJson);
        document.MarkQueued();

        _dbContext.FiscalDocuments.Add(document);
        _dbContext.RsGeWaybills.Add(waybill);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Persisted first, then queued. A publish failure (e.g. broker down) is not
        // fatal: the document is durably in Queued state and the recovery sweep will
        // re-enqueue it, honouring the rule that RS.GE never blocks business ops.
        try
        {
            await _publisher.PublishAsync(
                new RsGeSubmissionMessage { FiscalDocumentId = document.Id, Operation = RsGeOperation.SubmitWaybill },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Waybill {DocumentId} persisted but could not be queued; recovery sweep will retry",
                document.Id);
        }

        return Result.Success(new CreateWaybillResponse(document.Id, waybill.Id, document.Status.ToString()));
    }
}
