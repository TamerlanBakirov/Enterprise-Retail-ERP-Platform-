using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Products.DTOs;
using GeorgiaERP.Domain.Products;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Products.Commands;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<ProductDto>>
{
    private readonly IAppDbContext _dbContext;

    public CreateProductCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ProductDto>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var skuExists = await _dbContext.Products
            .AnyAsync(p => p.Sku == request.Sku, cancellationToken);

        if (skuExists)
            return Result.Failure<ProductDto>($"Product with SKU '{request.Sku}' already exists.");

        var categoryExists = await _dbContext.Categories
            .AnyAsync(c => c.Id == request.CategoryId, cancellationToken);

        if (!categoryExists)
            return Result.Failure<ProductDto>("Category not found.");

        var product = Product.Create(
            sku: request.Sku,
            name: request.Name,
            categoryId: request.CategoryId,
            unitOfMeasure: request.UnitOfMeasure,
            vatApplicable: request.VatApplicable,
            nameKa: request.NameKa,
            description: request.Description,
            weightKg: request.WeightKg,
            volumeL: request.VolumeL,
            widthCm: request.WidthCm,
            heightCm: request.HeightCm,
            depthCm: request.DepthCm,
            minStockLevel: request.MinStockLevel,
            maxStockLevel: request.MaxStockLevel,
            reorderPoint: request.ReorderPoint,
            reorderQty: request.ReorderQty,
            isSerialized: request.IsSerialized,
            isBatchTracked: request.IsBatchTracked,
            hasExpiry: request.HasExpiry);

        _dbContext.Products.Add(product);

        if (request.Barcodes is { Count: > 0 })
        {
            foreach (var bc in request.Barcodes)
            {
                var barcodeType = Enum.TryParse<BarcodeType>(bc.BarcodeType, true, out var bt) ? bt : BarcodeType.Internal;
                var barcode = ProductBarcode.Create(product.Id, bc.Barcode, barcodeType, bc.IsPrimary);
                _dbContext.ProductBarcodes.Add(barcode);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = new ProductDto(
            product.Id,
            product.Sku,
            product.Name,
            product.NameKa,
            product.Description,
            product.CategoryId,
            null,
            product.UnitOfMeasure,
            0m,
            product.VatApplicable ? 0.18m : 0m,
            product.VatApplicable,
            product.WeightKg,
            product.IsSerialized,
            product.IsBatchTracked,
            product.HasExpiry,
            product.IsActive,
            product.ImageUrl,
            product.CreatedAt,
            request.Barcodes?.Select(b => new ProductBarcodeDto(Guid.Empty, b.Barcode, b.BarcodeType, b.IsPrimary)).ToList()
                ?? [],
            []);

        return Result.Success(dto);
    }
}
