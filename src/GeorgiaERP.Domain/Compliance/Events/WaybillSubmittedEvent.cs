using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Compliance.Events;

/// <summary>
/// Raised when a waybill is created and queued for RS.GE submission.
/// Consumers can trigger stock reservation, notification, or audit logging.
/// </summary>
public sealed record WaybillSubmittedEvent : DomainEvent
{
    public Guid FiscalDocumentId { get; init; }
    public Guid WaybillId { get; init; }
    public string? SellerTin { get; init; }
    public string? BuyerTin { get; init; }
    public decimal? TotalAmount { get; init; }
}
