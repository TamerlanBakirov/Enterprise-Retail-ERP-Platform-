using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Pricing;

public class PriceListItem : BaseEntity
{
    public Guid PriceListId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? VariantId { get; private set; }
    public decimal Price { get; private set; }
    public decimal MinQty { get; private set; }

    // Navigation properties
    public PriceList PriceList { get; private set; } = default!;

    private PriceListItem() { }

    public static PriceListItem Create(Guid priceListId, Guid productId, decimal price, decimal minQty = 1, Guid? variantId = null)
    {
        return new PriceListItem
        {
            PriceListId = priceListId,
            ProductId = productId,
            VariantId = variantId,
            Price = price,
            MinQty = minQty
        };
    }
}
