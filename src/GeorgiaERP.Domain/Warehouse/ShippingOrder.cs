using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Warehouse;

public enum ShippingOrderStatus
{
    Draft,
    Picking,
    Packed,
    ReadyToShip,
    Shipped,
    Delivered,
    Cancelled
}

public enum ShippingOrderType
{
    SalesOrder,
    TransferOrder,
    Return,
    Manual
}

public class ShippingOrder : BaseEntity
{
    public string ShippingNumber { get; private set; } = default!;
    public Guid WarehouseId { get; private set; }
    public ShippingOrderStatus Status { get; private set; }
    public ShippingOrderType OrderType { get; private set; }
    public Guid? SourceOrderId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public Guid? DestWarehouseId { get; private set; }
    public string? ShippingAddress { get; private set; }
    public string? TrackingNumber { get; private set; }
    public string? Carrier { get; private set; }
    public DateTimeOffset? ExpectedShipDate { get; private set; }
    public DateTimeOffset? ShippedAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public Guid? ShippedBy { get; private set; }
    public Guid? RsGeWaybillId { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public ICollection<ShippingOrderLine> Lines { get; private set; } = new List<ShippingOrderLine>();

    private ShippingOrder() { }

    public static ShippingOrder Create(
        string shippingNumber, Guid warehouseId, ShippingOrderType orderType,
        Guid? sourceOrderId = null, Guid? customerId = null)
    {
        return new ShippingOrder
        {
            ShippingNumber = shippingNumber,
            WarehouseId = warehouseId,
            Status = ShippingOrderStatus.Draft,
            OrderType = orderType,
            SourceOrderId = sourceOrderId,
            CustomerId = customerId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void SetShippingDetails(string? address, string? carrier, DateTimeOffset? expectedShipDate)
    {
        ShippingAddress = address;
        Carrier = carrier;
        ExpectedShipDate = expectedShipDate;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetDestWarehouse(Guid? destWarehouseId)
    {
        DestWarehouseId = destWarehouseId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetNotes(string? notes)
    {
        Notes = notes;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void StartPicking()
    {
        if (Status != ShippingOrderStatus.Draft)
            throw new InvalidOperationException($"Cannot start picking: order is {Status}.");
        Status = ShippingOrderStatus.Picking;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkPacked()
    {
        if (Status != ShippingOrderStatus.Picking)
            throw new InvalidOperationException($"Cannot pack: order is {Status}.");
        Status = ShippingOrderStatus.Packed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkReadyToShip()
    {
        if (Status != ShippingOrderStatus.Packed)
            throw new InvalidOperationException($"Cannot mark ready: order is {Status}.");
        Status = ShippingOrderStatus.ReadyToShip;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Ship(Guid shippedBy, string? trackingNumber = null)
    {
        if (Status != ShippingOrderStatus.Packed && Status != ShippingOrderStatus.ReadyToShip)
            throw new InvalidOperationException($"Cannot ship: order is {Status}.");
        if (shippedBy == Guid.Empty)
            throw new ArgumentException("ShippedBy must not be empty.", nameof(shippedBy));
        Status = ShippingOrderStatus.Shipped;
        ShippedBy = shippedBy;
        ShippedAt = DateTimeOffset.UtcNow;
        TrackingNumber = trackingNumber;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkDelivered()
    {
        if (Status != ShippingOrderStatus.Shipped)
            throw new InvalidOperationException($"Cannot deliver: order is {Status}.");
        Status = ShippingOrderStatus.Delivered;
        DeliveredAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status == ShippingOrderStatus.Shipped || Status == ShippingOrderStatus.Delivered)
            throw new InvalidOperationException($"Cannot cancel: order is {Status}.");
        Status = ShippingOrderStatus.Cancelled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void LinkWaybill(Guid waybillId)
    {
        RsGeWaybillId = waybillId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
