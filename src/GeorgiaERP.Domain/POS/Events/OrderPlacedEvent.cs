using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.POS.Events;

/// <summary>
/// Raised when a POS transaction is completed (sale finalized).
/// Consumers can trigger inventory deductions, fiscal receipt generation,
/// loyalty point accrual, and daily closing updates.
/// </summary>
public sealed record OrderPlacedEvent : DomainEvent
{
    public Guid TransactionId { get; init; }
    public string TransactionNumber { get; init; } = default!;
    public Guid StoreId { get; init; }
    public Guid? CustomerId { get; init; }
    public decimal Total { get; init; }
    public decimal VatTotal { get; init; }
    public PosTransactionType TransactionType { get; init; }
}
