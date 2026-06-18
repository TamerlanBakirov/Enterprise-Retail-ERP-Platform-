using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Inventory;

public class StockLevel : BaseEntity
{
    public Guid ProductId { get; private set; }
    public Guid? VariantId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public string? LocationCode { get; private set; }
    public decimal QuantityOnHand { get; private set; }
    public decimal QuantityReserved { get; private set; }
    public decimal QuantityInTransit { get; private set; }
    public decimal CostPrice { get; private set; }
    public DateTimeOffset? LastCountDate { get; private set; }
    public byte[] RowVersion { get; private set; } = default!;
    public DateTimeOffset UpdatedAt { get; private set; }

    private StockLevel() { }

    public static StockLevel Create(Guid productId, Guid warehouseId, decimal costPrice = 0, Guid? variantId = null)
    {
        return new StockLevel
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            VariantId = variantId,
            CostPrice = costPrice,
            QuantityOnHand = 0,
            QuantityReserved = 0,
            QuantityInTransit = 0,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
