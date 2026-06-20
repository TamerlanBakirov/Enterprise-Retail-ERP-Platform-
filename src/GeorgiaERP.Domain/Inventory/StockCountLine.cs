using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Inventory;

public class StockCountLine : BaseEntity
{
    public Guid StockCountId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? VariantId { get; private set; }
    public decimal ExpectedQty { get; private set; }
    public decimal? CountedQty { get; private set; }
    public Guid? CountedBy { get; private set; }
    public DateTimeOffset? CountedAt { get; private set; }

    // Navigation properties
    public StockCount StockCount { get; private set; } = default!;

    private StockCountLine() { }

    public static StockCountLine Create(Guid stockCountId, Guid productId, decimal expectedQty, Guid? variantId = null)
    {
        return new StockCountLine
        {
            StockCountId = stockCountId,
            ProductId = productId,
            VariantId = variantId,
            ExpectedQty = expectedQty
        };
    }

    public void RecordCount(decimal countedQty, Guid countedBy)
    {
        CountedQty = countedQty;
        CountedBy = countedBy;
        CountedAt = DateTimeOffset.UtcNow;
    }

    public decimal Variance => (CountedQty ?? ExpectedQty) - ExpectedQty;
}
