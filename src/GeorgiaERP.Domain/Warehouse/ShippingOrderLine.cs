using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Warehouse;

public class ShippingOrderLine : BaseEntity
{
    public Guid ShippingOrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? VariantId { get; private set; }
    public decimal OrderedQty { get; private set; }
    public decimal PickedQty { get; private set; }
    public decimal PackedQty { get; private set; }
    public decimal ShippedQty { get; private set; }
    public Guid? PickLocationId { get; private set; }
    public string? BatchNumber { get; private set; }
    public string? SerialNumber { get; private set; }
    public string? Notes { get; private set; }

    public ShippingOrder ShippingOrder { get; private set; } = default!;

    private ShippingOrderLine() { }

    public static ShippingOrderLine Create(
        Guid shippingOrderId, Guid productId, decimal orderedQty, Guid? variantId = null)
    {
        return new ShippingOrderLine
        {
            ShippingOrderId = shippingOrderId,
            ProductId = productId,
            VariantId = variantId,
            OrderedQty = orderedQty,
            PickedQty = 0,
            PackedQty = 0,
            ShippedQty = 0
        };
    }

    public void Pick(decimal qty, Guid? locationId = null)
    {
        PickedQty = qty;
        PickLocationId = locationId;
    }

    public void Pack(decimal qty) => PackedQty = qty;
    public void SetShippedQty(decimal qty) => ShippedQty = qty;

    public void SetBatch(string? batchNumber, string? serialNumber = null)
    {
        BatchNumber = batchNumber;
        SerialNumber = serialNumber;
    }

    public void SetNotes(string? notes) => Notes = notes;
}
