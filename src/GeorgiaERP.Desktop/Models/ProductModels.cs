namespace GeorgiaERP.Desktop.Models;

public record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string? NameKa,
    string? Description,
    Guid CategoryId,
    string? CategoryName,
    string UnitOfMeasure,
    decimal RetailPrice,
    decimal? WholesalePrice,
    decimal VatRate,
    string? Barcode,
    bool TrackSerials,
    bool TrackBatches,
    bool TrackExpiry,
    decimal? Weight,
    bool IsActive,
    DateTimeOffset CreatedAt);

public record CategoryDto(
    Guid Id,
    string Code,
    string Name,
    string? NameKa,
    Guid? ParentId,
    bool IsActive);

// Matches the API CreateProductRequest. Price is NOT part of product creation
// (it lives in the Pricing module); only the catalog fields + barcodes are sent.
public record CreateProductRequest(
    string Sku,
    string Name,
    string? NameKa,
    string? Description,
    Guid CategoryId,
    string UnitOfMeasure,
    bool VatApplicable,
    decimal? WeightKg,
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
    decimal? RetailPrice,
    decimal? WholesalePrice,
    decimal? VatRate,
    bool? IsActive);
