using GeorgiaERP.Domain.Common;
using GeorgiaERP.Domain.Products.Events;

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
    public string? ImageUrl { get; private set; }

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
        string? description = null,
        decimal? weightKg = null,
        decimal? volumeL = null,
        decimal? widthCm = null,
        decimal? heightCm = null,
        decimal? depthCm = null,
        decimal? minStockLevel = null,
        decimal? maxStockLevel = null,
        decimal? reorderPoint = null,
        decimal? reorderQty = null,
        bool isSerialized = false,
        bool isBatchTracked = false,
        bool hasExpiry = false)
    {
        var product = new Product
        {
            Sku = sku,
            Name = name,
            NameKa = nameKa,
            Description = description,
            CategoryId = categoryId,
            UnitOfMeasure = unitOfMeasure,
            VatApplicable = vatApplicable,
            WeightKg = weightKg,
            VolumeL = volumeL,
            WidthCm = widthCm,
            HeightCm = heightCm,
            DepthCm = depthCm,
            MinStockLevel = minStockLevel,
            MaxStockLevel = maxStockLevel,
            ReorderPoint = reorderPoint,
            ReorderQty = reorderQty,
            IsSerialized = isSerialized,
            IsBatchTracked = isBatchTracked,
            HasExpiry = hasExpiry,
            IsActive = true
        };

        product.RaiseDomainEvent(new ProductCreatedEvent
        {
            ProductId = product.Id,
            Sku = sku,
            Name = name,
            CategoryId = categoryId,
            VatApplicable = vatApplicable
        });

        return product;
    }

    public void Update(
        string? name = null,
        string? nameKa = null,
        string? description = null,
        Guid? categoryId = null,
        string? unitOfMeasure = null,
        bool? vatApplicable = null,
        decimal? weightKg = null,
        decimal? minStockLevel = null,
        decimal? maxStockLevel = null,
        decimal? reorderPoint = null,
        decimal? reorderQty = null)
    {
        if (name is not null) Name = name;
        if (nameKa is not null) NameKa = nameKa;
        if (description is not null) Description = description;
        if (categoryId.HasValue) CategoryId = categoryId.Value;
        if (unitOfMeasure is not null) UnitOfMeasure = unitOfMeasure;
        if (vatApplicable.HasValue) VatApplicable = vatApplicable.Value;
        if (weightKg.HasValue) WeightKg = weightKg.Value;
        if (minStockLevel.HasValue) MinStockLevel = minStockLevel.Value;
        if (maxStockLevel.HasValue) MaxStockLevel = maxStockLevel.Value;
        if (reorderPoint.HasValue) ReorderPoint = reorderPoint.Value;
        if (reorderQty.HasValue) ReorderQty = reorderQty.Value;
    }

    public void SetImageUrl(string? imageUrl) => ImageUrl = imageUrl;

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
