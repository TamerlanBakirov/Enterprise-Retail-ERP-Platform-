using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Warehouse;

public class ReceivingOrderLine : BaseEntity
{
    public Guid ReceivingOrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? VariantId { get; private set; }
    public decimal ExpectedQty { get; private set; }
    public decimal ReceivedQty { get; private set; }
    public decimal? DamagedQty { get; private set; }
    public string? BatchNumber { get; private set; }
    public string? SerialNumber { get; private set; }
    public DateOnly? ExpiryDate { get; private set; }
    public Guid? LocationId { get; private set; }
    public string? Notes { get; private set; }

    public ReceivingOrder ReceivingOrder { get; private set; } = default!;

    private ReceivingOrderLine() { }

    public static ReceivingOrderLine Create(
        Guid receivingOrderId, Guid productId, decimal expectedQty, Guid? variantId = null)
    {
        return new ReceivingOrderLine
        {
            ReceivingOrderId = receivingOrderId,
            ProductId = productId,
            VariantId = variantId,
            ExpectedQty = expectedQty,
            ReceivedQty = 0
        };
    }

    public void Receive(decimal receivedQty, decimal? damagedQty = null)
    {
        ReceivedQty = receivedQty;
        DamagedQty = damagedQty;
    }

    public void SetBatch(string? batchNumber, string? serialNumber = null, DateOnly? expiryDate = null)
    {
        BatchNumber = batchNumber;
        SerialNumber = serialNumber;
        ExpiryDate = expiryDate;
    }

    public void SetLocation(Guid? locationId) => LocationId = locationId;
    public void SetNotes(string? notes) => Notes = notes;
}
