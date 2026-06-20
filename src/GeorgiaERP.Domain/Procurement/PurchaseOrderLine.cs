using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Procurement;

public class PurchaseOrderLine : BaseEntity
{
    public Guid PurchaseOrderId { get; private set; }
    public int LineNumber { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? VariantId { get; private set; }
    public decimal OrderedQty { get; private set; }
    public decimal ReceivedQty { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal VatAmount { get; private set; }
    public decimal LineTotal { get; private set; }

    // Navigation properties
    public PurchaseOrder PurchaseOrder { get; private set; } = default!;

    private PurchaseOrderLine() { }

    public static PurchaseOrderLine Create(
        Guid purchaseOrderId,
        int lineNumber,
        Guid productId,
        decimal orderedQty,
        decimal unitPrice,
        Guid? variantId = null)
    {
        return new PurchaseOrderLine
        {
            PurchaseOrderId = purchaseOrderId,
            LineNumber = lineNumber,
            ProductId = productId,
            VariantId = variantId,
            OrderedQty = orderedQty,
            UnitPrice = unitPrice,
            ReceivedQty = 0
        };
    }

    public void SetVat(decimal vatAmount) => VatAmount = vatAmount;
    public void SetLineTotal(decimal total) => LineTotal = total;
    public void AddReceivedQty(decimal qty) => ReceivedQty += qty;
    public decimal RemainingQty => OrderedQty - ReceivedQty;
}
