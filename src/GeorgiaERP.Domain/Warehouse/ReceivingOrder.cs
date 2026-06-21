using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Warehouse;

public enum ReceivingOrderStatus
{
    Expected,
    InProgress,
    Completed,
    Cancelled
}

public enum ReceivingOrderSource
{
    PurchaseOrder,
    TransferOrder,
    Return,
    Manual
}

public class ReceivingOrder : BaseEntity
{
    public string ReceivingNumber { get; private set; } = default!;
    public Guid WarehouseId { get; private set; }
    public ReceivingOrderStatus Status { get; private set; }
    public ReceivingOrderSource Source { get; private set; }
    public Guid? SourceOrderId { get; private set; }
    public Guid? SupplierId { get; private set; }
    public DateTimeOffset? ExpectedDate { get; private set; }
    public DateTimeOffset? ReceivedAt { get; private set; }
    public Guid? ReceivedBy { get; private set; }
    public Guid? LocationId { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public ICollection<ReceivingOrderLine> Lines { get; private set; } = new List<ReceivingOrderLine>();

    private ReceivingOrder() { }

    public static ReceivingOrder Create(
        string receivingNumber, Guid warehouseId, ReceivingOrderSource source,
        Guid? sourceOrderId = null, Guid? supplierId = null)
    {
        return new ReceivingOrder
        {
            ReceivingNumber = receivingNumber,
            WarehouseId = warehouseId,
            Status = ReceivingOrderStatus.Expected,
            Source = source,
            SourceOrderId = sourceOrderId,
            SupplierId = supplierId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void SetExpectedDate(DateTimeOffset? date)
    {
        ExpectedDate = date;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetLocation(Guid? locationId)
    {
        LocationId = locationId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetNotes(string? notes)
    {
        Notes = notes;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void StartReceiving()
    {
        if (Status != ReceivingOrderStatus.Expected)
            throw new InvalidOperationException($"Cannot start receiving: order is {Status}.");
        Status = ReceivingOrderStatus.InProgress;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Complete(Guid receivedBy)
    {
        if (Status != ReceivingOrderStatus.InProgress)
            throw new InvalidOperationException($"Cannot complete: order is {Status}.");
        if (receivedBy == Guid.Empty)
            throw new ArgumentException("ReceivedBy must not be empty.", nameof(receivedBy));
        Status = ReceivingOrderStatus.Completed;
        ReceivedBy = receivedBy;
        ReceivedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status == ReceivingOrderStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a completed order.");
        Status = ReceivingOrderStatus.Cancelled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
