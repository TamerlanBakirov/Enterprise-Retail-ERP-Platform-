using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Compliance.Events;

/// <summary>
/// Raised when RS.GE confirms a waybill, signalling that the fiscal document
/// is legally accepted and goods movement is officially recorded.
/// </summary>
public sealed record WaybillConfirmedEvent : DomainEvent
{
    public Guid FiscalDocumentId { get; init; }
    public Guid WaybillId { get; init; }
    public string? WaybillNumber { get; init; }
}
