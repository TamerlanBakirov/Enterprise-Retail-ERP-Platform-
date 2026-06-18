using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Products;

public class ProductVariant : BaseEntity
{
    public Guid ProductId { get; private set; }
    public string Sku { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Attributes { get; private set; } // jsonb
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // Navigation properties
    public Product Product { get; private set; } = default!;

    private ProductVariant() { }

    public static ProductVariant Create(Guid productId, string sku, string name, string? attributes = null)
    {
        return new ProductVariant
        {
            ProductId = productId,
            Sku = sku,
            Name = name,
            Attributes = attributes,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
