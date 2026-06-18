using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Procurement;

public class GoodsReceiptLine : BaseEntity
{
    public Guid GrnId { get; private set; }
    public Guid PoLineId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? VariantId { get; private set; }
    public decimal ReceivedQty { get; private set; }
    public decimal AcceptedQty { get; private set; }
    public decimal RejectedQty { get; private set; }
    public string? BatchNumber { get; private set; }
    public DateTimeOffset? ExpiryDate { get; private set; }
    public decimal UnitCost { get; private set; }

    // Navigation properties
    public GoodsReceiptNote GoodsReceiptNote { get; private set; } = default!;
    public PurchaseOrderLine PurchaseOrderLine { get; private set; } = default!;

    private GoodsReceiptLine() { }

    public static GoodsReceiptLine Create(
        Guid grnId,
        Guid poLineId,
        Guid productId,
        decimal receivedQty,
        decimal unitCost,
        Guid? variantId = null)
    {
        return new GoodsReceiptLine
        {
            GrnId = grnId,
            PoLineId = poLineId,
            ProductId = productId,
            VariantId = variantId,
            ReceivedQty = receivedQty,
            AcceptedQty = receivedQty,
            RejectedQty = 0,
            UnitCost = unitCost
        };
    }
}
