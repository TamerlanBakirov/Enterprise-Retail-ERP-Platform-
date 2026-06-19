using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Inventory;

public enum CountType
{
    Full,
    Partial,
    Cycle
}

public enum StockCountStatus
{
    Draft,
    InProgress,
    Completed,
    Cancelled
}

public class StockCount : BaseEntity
{
    public Guid WarehouseId { get; private set; }
    public CountType CountType { get; private set; }
    public StockCountStatus Status { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // Navigation properties
    public ICollection<StockCountLine> Lines { get; private set; } = new List<StockCountLine>();

    private StockCount() { }

    public static StockCount Create(Guid warehouseId, CountType countType, Guid createdBy)
    {
        return new StockCount
        {
            WarehouseId = warehouseId,
            CountType = countType,
            Status = StockCountStatus.Draft,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Start()
    {
        Status = StockCountStatus.InProgress;
        StartedAt = DateTimeOffset.UtcNow;
    }

    public void Complete(Guid approvedBy)
    {
        Status = StockCountStatus.Completed;
        ApprovedBy = approvedBy;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        Status = StockCountStatus.Cancelled;
    }
}
