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
    Guid CreatedBy) : IRequest<Result<ProductDto>>;
