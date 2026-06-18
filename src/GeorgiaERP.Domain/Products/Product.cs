using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Products;

public class Product : AuditableEntity
{
    public string Sku { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? NameKa { get; private set; }
    public string? Description { get; private set; }
    public Guid CategoryId { get; private set; }
    public string UnitOfMeasure { get; private set; } = default!;
    public string? RsGeUnitId { get; private set; }
    public bool VatApplicable { get; private set; }
    public string? ExciseCode { get; private set; }
    public decimal? WeightKg { get; private set; }
    public decimal? VolumeL { get; private set; }
    public decimal? WidthCm { get; private set; }
    public decimal? HeightCm { get; private set; }
    public decimal? DepthCm { get; private set; }
    public decimal? MinStockLevel { get; private set; }
    public decimal? MaxStockLevel { get; private set; }
    public decimal? ReorderPoint { get; private set; }
    public decimal? ReorderQty { get; private set; }
    public bool IsSerialized { get; private set; }
    public bool IsBatchTracked { get; private set; }
    public bool HasExpiry { get; private set; }
    public bool IsActive { get; private set; }

    // Navigation properties
    public Category Category { get; private set; } = default!;
    public ICollection<ProductVariant> Variants { get; private set; } = new List<ProductVariant>();
    public ICollection<ProductBarcode> Barcodes { get; private set; } = new List<ProductBarcode>();

    private Product() { }

    public static Product Create(
        string sku,
        string name,
        Guid categoryId,
        string unitOfMeasure,
        bool vatApplicable = true,
        string? nameKa = null,
        string? description = null)
    {
        return new Product
        {
            Sku = sku,
            Name = name,
            NameKa = nameKa,
            Description = description,
            CategoryId = categoryId,
            UnitOfMeasure = unitOfMeasure,
            VatApplicable = vatApplicable,
            IsActive = true
        };
    }
}
