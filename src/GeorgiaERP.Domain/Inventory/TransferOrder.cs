using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Inventory;

public enum TransferOrderStatus
{
    Draft,
    PendingApproval,
    Approved,
    InTransit,
    Received,
    Cancelled
}

public class TransferOrder : BaseEntity
{
    public string TransferNumber { get; private set; } = default!;
    public Guid SourceWarehouseId { get; private set; }
    public Guid DestWarehouseId { get; private set; }
    public TransferOrderStatus Status { get; private set; }
    public Guid? RsGeWaybillId { get; private set; }
    public Guid RequestedBy { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTimeOffset? ShippedAt { get; private set; }
    public DateTimeOffset? ReceivedAt { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Navigation properties
    public ICollection<TransferOrderLine> Lines { get; private set; } = new List<TransferOrderLine>();

    private TransferOrder() { }

    public static TransferOrder Create(string transferNumber, Guid sourceWarehouseId, Guid destWarehouseId, Guid requestedBy)
    {
        return new TransferOrder
        {
            TransferNumber = transferNumber,
            SourceWarehouseId = sourceWarehouseId,
            DestWarehouseId = destWarehouseId,
            Status = TransferOrderStatus.Draft,
            RequestedBy = requestedBy,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
