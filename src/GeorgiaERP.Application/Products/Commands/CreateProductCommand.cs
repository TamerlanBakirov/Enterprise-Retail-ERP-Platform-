using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Products.DTOs;
using MediatR;

namespace GeorgiaERP.Application.Products.Commands;

public record CreateProductCommand(
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
    List<CreateBarcodeRequest>? Barcodes,
    Guid CreatedBy) : IRequest<Result<ProductDto>>, ICacheInvalidator
{
    // Invalidate category caches (product counts change) and dashboard KPIs.
    // Product list caches use parameterized keys and expire via 2-minute TTL.
    public IReadOnlyList<string> CacheKeysToInvalidate =>
        [$"products:categories:{CategoryId}:True",
         $"products:categories:{CategoryId}:False",
         $"products:categories:{CategoryId}:",
         $"products:categories::True",
         $"products:categories::False",
         $"products:categories::",
         "dashboard:kpi"];
}
