using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.POS.Events;

/// <summary>
/// Raised when a payment is recorded against a POS transaction.
/// Consumers can trigger bank reconciliation entries, cash drawer updates,
/// or card settlement tracking.
/// </summary>
public sealed record PaymentProcessedEvent : DomainEvent
{
    public Guid PaymentId { get; init; }
    public Guid TransactionId { get; init; }
    public PaymentMethod PaymentMethod { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "GEL";
}
