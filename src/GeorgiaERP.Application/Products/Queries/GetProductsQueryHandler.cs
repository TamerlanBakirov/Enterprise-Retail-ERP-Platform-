using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Products.DTOs;
using GeorgiaERP.Domain.Pricing;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Products.Queries;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetProductsQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(p =>
                p.Sku.ToLower().Contains(search) ||
                p.Name.ToLower().Contains(search) ||
                (p.NameKa != null && p.NameKa.ToLower().Contains(search)) ||
                p.Barcodes.Any(b => b.Barcode.Contains(search)));
        }

        if (request.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);

        if (request.IsActive.HasValue)
            query = query.Where(p => p.IsActive == request.IsActive.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var products = await query
            .OrderBy(p => p.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .AsSplitQuery()
            .Select(p => new ProductDto(
                p.Id,
                p.Sku,
                p.Name,
                p.NameKa,
                p.Description,
                p.CategoryId,
                p.Category.Name,
                p.UnitOfMeasure,
                _dbContext.PriceListItems
                    .Where(i => i.ProductId == p.Id && i.VariantId == null &&
                        _dbContext.PriceLists.Any(pl => pl.Id == i.PriceListId
                            && pl.PriceType == PriceType.Retail && pl.IsActive))
                    .OrderBy(i => i.MinQty)
                    .Select(i => i.Price)
                    .FirstOrDefault(),
                p.VatApplicable ? 0.18m : 0m,
                p.VatApplicable,
                p.WeightKg,
                p.IsSerialized,
                p.IsBatchTracked,
                p.HasExpiry,
                p.IsActive,
                p.ImageUrl,
                p.CreatedAt,
                p.Barcodes.Select(b => new ProductBarcodeDto(b.Id, b.Barcode, b.BarcodeType.ToString(), b.IsPrimary)).ToList(),
                p.Variants.Select(v => new ProductVariantDto(v.Id, v.Sku, v.Name, v.Attributes, v.IsActive)).ToList()))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductDto>
        {
            Items = products,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    private readonly IAppDbContext _dbContext;

    public GetProductByIdQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .Where(p => p.Id == request.Id)
            .AsSplitQuery()
            .Select(p => new ProductDto(
                p.Id,
                p.Sku,
                p.Name,
                p.NameKa,
                p.Description,
                p.CategoryId,
                p.Category.Name,
                p.UnitOfMeasure,
                _dbContext.PriceListItems
                    .Where(i => i.ProductId == p.Id && i.VariantId == null &&
                        _dbContext.PriceLists.Any(pl => pl.Id == i.PriceListId
                            && pl.PriceType == PriceType.Retail && pl.IsActive))
                    .OrderBy(i => i.MinQty)
                    .Select(i => i.Price)
                    .FirstOrDefault(),
                p.VatApplicable ? 0.18m : 0m,
                p.VatApplicable,
                p.WeightKg,
                p.IsSerialized,
                p.IsBatchTracked,
                p.HasExpiry,
                p.IsActive,
                p.ImageUrl,
                p.CreatedAt,
                p.Barcodes.Select(b => new ProductBarcodeDto(b.Id, b.Barcode, b.BarcodeType.ToString(), b.IsPrimary)).ToList(),
                p.Variants.Select(v => new ProductVariantDto(v.Id, v.Sku, v.Name, v.Attributes, v.IsActive)).ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }
}

public class GetProductByBarcodeQueryHandler : IRequestHandler<GetProductByBarcodeQuery, ProductDto?>
{
    private readonly IAppDbContext _dbContext;

    public GetProductByBarcodeQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProductDto?> Handle(GetProductByBarcodeQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.ProductBarcodes
            .AsNoTracking()
            .Where(b => b.Barcode == request.Barcode)
            .Select(b => b.Product)
            .Select(p => new ProductDto(
                p.Id,
                p.Sku,
                p.Name,
                p.NameKa,
                p.Description,
                p.CategoryId,
                p.Category.Name,
                p.UnitOfMeasure,
                _dbContext.PriceListItems
                    .Where(i => i.ProductId == p.Id && i.VariantId == null &&
                        _dbContext.PriceLists.Any(pl => pl.Id == i.PriceListId
                            && pl.PriceType == PriceType.Retail && pl.IsActive))
                    .OrderBy(i => i.MinQty)
                    .Select(i => i.Price)
                    .FirstOrDefault(),
                p.VatApplicable ? 0.18m : 0m,
                p.VatApplicable,
                p.WeightKg,
                p.IsSerialized,
                p.IsBatchTracked,
                p.HasExpiry,
                p.IsActive,
                p.ImageUrl,
                p.CreatedAt,
                p.Barcodes.Select(bc => new ProductBarcodeDto(bc.Id, bc.Barcode, bc.BarcodeType.ToString(), bc.IsPrimary)).ToList(),
                p.Variants.Select(v => new ProductVariantDto(v.Id, v.Sku, v.Name, v.Attributes, v.IsActive)).ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }
}

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetCategoriesQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Categories.AsNoTracking();

        if (request.ParentId.HasValue)
            query = query.Where(c => c.ParentId == request.ParentId.Value);

        if (request.IsActive.HasValue)
            query = query.Where(c => c.IsActive == request.IsActive.Value);

        return await query
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new CategoryDto(
                c.Id,
                c.ParentId,
                c.Code,
                c.Name,
                c.NameKa,
                c.SortOrder,
                c.IsActive,
                c.Products.Count))
            .ToListAsync(cancellationToken);
    }
}
