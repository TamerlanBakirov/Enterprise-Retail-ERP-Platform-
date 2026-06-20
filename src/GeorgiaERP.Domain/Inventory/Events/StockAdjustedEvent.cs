using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Inventory.Events;

/// <summary>
/// Raised when stock levels change (receipt, dispatch, adjustment, transfer).
/// Consumers can trigger reorder checks, analytics updates, or alerts
/// when stock drops below minimum threshold.
/// </summary>
public sealed record StockAdjustedEvent : DomainEvent
{
    public Guid ProductId { get; init; }
    public Guid WarehouseId { get; init; }
    public Guid? VariantId { get; init; }
    public decimal QuantityChange { get; init; }
    public decimal NewQuantityOnHand { get; init; }
    public MovementType MovementType { get; init; }
    public Guid? ReferenceId { get; init; }
}
