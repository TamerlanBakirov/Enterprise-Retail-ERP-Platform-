using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Inventory;

public class TransferOrderLine : BaseEntity
{
    public Guid TransferOrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? VariantId { get; private set; }
    public decimal RequestedQty { get; private set; }
    public decimal? ShippedQty { get; private set; }
    public decimal? ReceivedQty { get; private set; }
    public string? BatchNumber { get; private set; }
    public string? SerialNumber { get; private set; }

    // Navigation properties
    public TransferOrder TransferOrder { get; private set; } = default!;

    private TransferOrderLine() { }

    public static TransferOrderLine Create(Guid transferOrderId, Guid productId, decimal requestedQty, Guid? variantId = null)
    {
        return new TransferOrderLine
        {
            TransferOrderId = transferOrderId,
            ProductId = productId,
            VariantId = variantId,
            RequestedQty = requestedQty
        };
    }
}
