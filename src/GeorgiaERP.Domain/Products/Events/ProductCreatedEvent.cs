using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Products.Events;

/// <summary>
/// Raised when a new product is created in the catalog.
/// Consumers can trigger initial stock level creation, price list updates,
/// or synchronization with external systems.
/// </summary>
public sealed record ProductCreatedEvent : DomainEvent
{
    public Guid ProductId { get; init; }
    public string Sku { get; init; } = default!;
    public string Name { get; init; } = default!;
    public Guid CategoryId { get; init; }
    public bool VatApplicable { get; init; }
}
