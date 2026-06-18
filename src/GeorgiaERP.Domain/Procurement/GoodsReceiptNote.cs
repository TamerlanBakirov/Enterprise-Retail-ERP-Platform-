using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Procurement;

public enum GoodsReceiptStatus
{
    Draft,
    Completed,
    Cancelled
}

public class GoodsReceiptNote : BaseEntity
{
    public string GrnNumber { get; private set; } = default!;
    public Guid PurchaseOrderId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid SupplierId { get; private set; }
    public Guid? RsGeWaybillId { get; private set; }
    public DateTimeOffset ReceiptDate { get; private set; }
    public GoodsReceiptStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public Guid ReceivedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // Navigation properties
    public PurchaseOrder PurchaseOrder { get; private set; } = default!;
    public Supplier Supplier { get; private set; } = default!;
    public ICollection<GoodsReceiptLine> Lines { get; private set; } = new List<GoodsReceiptLine>();

    private GoodsReceiptNote() { }

    public static GoodsReceiptNote Create(
        string grnNumber,
        Guid purchaseOrderId,
        Guid warehouseId,
        Guid supplierId,
        Guid receivedBy)
    {
        return new GoodsReceiptNote
        {
            GrnNumber = grnNumber,
            PurchaseOrderId = purchaseOrderId,
            WarehouseId = warehouseId,
            SupplierId = supplierId,
            ReceivedBy = receivedBy,
            ReceiptDate = DateTimeOffset.UtcNow,
            Status = GoodsReceiptStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
