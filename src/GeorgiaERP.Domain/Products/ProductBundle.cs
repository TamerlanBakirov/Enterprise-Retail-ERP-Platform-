using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Products;

public class ProductBundle : BaseEntity
{
    public Guid BundleProductId { get; private set; }
    public Guid ComponentProductId { get; private set; }
    public decimal Quantity { get; private set; }

    // Navigation properties
    public Product BundleProduct { get; private set; } = default!;
    public Product ComponentProduct { get; private set; } = default!;

    private ProductBundle() { }

    public static ProductBundle Create(Guid bundleProductId, Guid componentProductId, decimal quantity)
    {
        return new ProductBundle
        {
            BundleProductId = bundleProductId,
            ComponentProductId = componentProductId,
            Quantity = quantity
        };
    }
}
