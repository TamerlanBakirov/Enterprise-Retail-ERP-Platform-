namespace GeorgiaERP.Application.Products.DTOs;

public record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string? NameKa,
    string? Description,
    Guid CategoryId,
    string? CategoryName,
    string UnitOfMeasure,
    bool VatApplicable,
    decimal? WeightKg,
    bool IsSerialized,
    bool IsBatchTracked,
    bool HasExpiry,
    bool IsActive,
    string? ImageUrl,
    DateTimeOffset CreatedAt,
    IReadOnlyList<ProductBarcodeDto> Barcodes,
    IReadOnlyList<ProductVariantDto> Variants);

public record ProductBarcodeDto(Guid Id, string Barcode, string BarcodeType, bool IsPrimary);

public record ProductVariantDto(Guid Id, string Sku, string Name, string? Attributes, bool IsActive);

public record CreateProductRequest(
    string Sku,
    string Name,
    string? NameKa,
    string? Description,
    Guid CategoryId,
    string UnitOfMeasure,
    bool VatApplicable,
    decimal? WeightKg,
    decimal? VolumeL,
    decimal? WidthCm,
    decimal? HeightCm,
    decimal? DepthCm,
    decimal? MinStockLevel,
    decimal? MaxStockLevel,
    decimal? ReorderPoint,
    decimal? ReorderQty,
    bool IsSerialized,
    bool IsBatchTracked,
    bool HasExpiry,
    List<CreateBarcodeRequest>? Barcodes);

public record CreateBarcodeRequest(string Barcode, string BarcodeType, bool IsPrimary);

public record UpdateProductRequest(
    string? Name,
    string? NameKa,
    string? Description,
    Guid? CategoryId,
    string? UnitOfMeasure,
    bool? VatApplicable,
    decimal? WeightKg,
    decimal? MinStockLevel,
    decimal? MaxStockLevel,
    decimal? ReorderPoint,
    decimal? ReorderQty,
    bool? IsActive);

public record CategoryDto(
    Guid Id,
    Guid? ParentId,
    string Code,
    string Name,
    string? NameKa,
    int SortOrder,
    bool IsActive,
    int ProductCount);

public record CreateCategoryRequest(
    Guid? ParentId,
    string Code,
    string Name,
    string? NameKa,
    int SortOrder);
