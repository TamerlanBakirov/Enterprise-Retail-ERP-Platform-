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

public record CreateProductRequest(
    string Sku,
    string Name,
    string? NameKa,
    string? Description,
    Guid CategoryId,
    string UnitOfMeasure,
    decimal RetailPrice,
    decimal? WholesalePrice,
    decimal VatRate,
    string? Barcode,
    bool TrackSerials,
    bool TrackBatches,
    bool TrackExpiry,
    decimal? Weight);
