using GeorgiaERP.Application.Common;
using MediatR;

namespace GeorgiaERP.Application.Compliance.Commands;

/// <summary>
/// Creates a waybill fiscal document and enqueues it for asynchronous submission
/// to RS.GE. Returns immediately once the document is persisted and queued — the
/// actual Revenue Service round-trip happens in the worker, so a slow or
/// unavailable RS.GE never blocks the originating business operation.
/// </summary>
public record CreateWaybillCommand(
    int WaybillType,
    string BuyerTin,
    string? BuyerName,
    string? SellerTin,
    string? SellerName,
    string StartAddress,
    string EndAddress,
    string? VehicleNumber,
    string? DriverTin,
    string? TransportType,
    string? InternalRef,
    Guid? ReferenceId,
    string? ReferenceType,
    List<WaybillGoodsItem> Goods) : IRequest<Result<CreateWaybillResponse>>;

public record CreateWaybillResponse(
    Guid FiscalDocumentId,
    Guid WaybillId,
    string Status);
