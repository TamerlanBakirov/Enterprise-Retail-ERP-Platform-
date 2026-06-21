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

    public void SetTotals(decimal subtotal, decimal vatTotal, decimal total)
    {
        if (subtotal < 0) throw new InvalidOperationException("Subtotal cannot be negative.");
        if (vatTotal < 0) throw new InvalidOperationException("VAT total cannot be negative.");
        if (total < 0) throw new InvalidOperationException("Total cannot be negative.");
        Subtotal = subtotal; VatTotal = vatTotal; Total = total; Touch();
    }

    public void SetExpectedDate(DateTimeOffset? date) { ExpectedDate = date; Touch(); }
    public void SetNotes(string? notes) { Notes = notes; Touch(); }

    public void Approve(Guid approvedBy)
    {
        if (Status != PurchaseOrderStatus.Draft && Status != PurchaseOrderStatus.PendingApproval)
            throw new InvalidOperationException(
                $"Cannot approve PO in '{Status}' status. Must be Draft or PendingApproval.");
        Status = PurchaseOrderStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    public void Send()
    {
        if (Status != PurchaseOrderStatus.Approved)
            throw new InvalidOperationException(
                $"Cannot send PO in '{Status}' status. Must be Approved.");
        Status = PurchaseOrderStatus.Sent;
        Touch();
    }

    public void MarkPartiallyReceived()
    {
        if (Status != PurchaseOrderStatus.Sent && Status != PurchaseOrderStatus.PartiallyReceived)
            throw new InvalidOperationException(
                $"Cannot mark PO as partially received in '{Status}' status.");
        Status = PurchaseOrderStatus.PartiallyReceived;
        Touch();
    }

    public void MarkReceived()
    {
        if (Status != PurchaseOrderStatus.Sent && Status != PurchaseOrderStatus.PartiallyReceived)
            throw new InvalidOperationException(
                $"Cannot mark PO as received in '{Status}' status.");
        Status = PurchaseOrderStatus.Received;
        Touch();
    }

    public void Cancel()
    {
        if (Status == PurchaseOrderStatus.Received)
            throw new InvalidOperationException("Cannot cancel a fully received purchase order.");
        Status = PurchaseOrderStatus.Cancelled;
        Touch();
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
