using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Inventory;

public enum MovementType
{
    Receipt,
    Dispatch,
    TransferIn,
    TransferOut,
    Adjustment,
    Sale,
    Return
}

public class StockMovement : BaseEntity
{
    public MovementType MovementType { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? VariantId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal CostPrice { get; private set; }
    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public string? BatchNumber { get; private set; }
    public string? SerialNumber { get; private set; }
    public DateTimeOffset? ExpiryDate { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }

    private StockMovement() { }

    public static StockMovement Create(
        MovementType movementType,
        Guid productId,
        Guid warehouseId,
        decimal quantity,
        decimal costPrice,
        Guid createdBy,
        Guid? variantId = null)
    {
        return new StockMovement
        {
            MovementType = movementType,
            ProductId = productId,
            WarehouseId = warehouseId,
            Quantity = quantity,
            CostPrice = costPrice,
            VariantId = variantId,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
