using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Products;

public enum BarcodeType
{
    Ean13,
    Ean8,
    Upc,
    Code128,
    Internal
}

public class ProductBarcode : BaseEntity
{
    public Guid ProductId { get; private set; }
    public Guid? VariantId { get; private set; }
    public string Barcode { get; private set; } = default!;
    public BarcodeType BarcodeType { get; private set; }
    public bool IsPrimary { get; private set; }

    // Navigation properties
    public Product Product { get; private set; } = default!;
    public ProductVariant? Variant { get; private set; }

    private ProductBarcode() { }

    public static ProductBarcode Create(Guid productId, string barcode, BarcodeType barcodeType, bool isPrimary = false, Guid? variantId = null)
    {
        return new ProductBarcode
        {
            ProductId = productId,
            VariantId = variantId,
            Barcode = barcode,
            BarcodeType = barcodeType,
            IsPrimary = isPrimary
        };
    }
}
