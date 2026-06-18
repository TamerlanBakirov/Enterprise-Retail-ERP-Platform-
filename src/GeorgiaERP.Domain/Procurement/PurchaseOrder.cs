using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Procurement;

public enum PurchaseOrderStatus
{
    Draft,
    PendingApproval,
    Approved,
    Sent,
    PartiallyReceived,
    Received,
    Cancelled
}

public class PurchaseOrder : BaseEntity
{
    public string PoNumber { get; private set; } = default!;
    public Guid SupplierId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public PurchaseOrderStatus Status { get; private set; }
    public DateTimeOffset OrderDate { get; private set; }
    public DateTimeOffset? ExpectedDate { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal VatTotal { get; private set; }
    public decimal Total { get; private set; }
    public string? Notes { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Navigation properties
    public Supplier Supplier { get; private set; } = default!;
    public ICollection<PurchaseOrderLine> Lines { get; private set; } = new List<PurchaseOrderLine>();

    private PurchaseOrder() { }

    public static PurchaseOrder Create(string poNumber, Guid supplierId, Guid warehouseId, Guid createdBy)
    {
        return new PurchaseOrder
        {
            PoNumber = poNumber,
            SupplierId = supplierId,
            WarehouseId = warehouseId,
            Status = PurchaseOrderStatus.Draft,
            OrderDate = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
