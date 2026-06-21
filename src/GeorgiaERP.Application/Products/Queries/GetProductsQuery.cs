using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Products.DTOs;
using MediatR;

namespace GeorgiaERP.Application.Products.Queries;

/// <summary>
/// Searches products by SKU, name, Georgian name, or barcode.
/// Cached for 2 minutes per unique parameter combination.
/// </summary>
public record GetProductsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    Guid? CategoryId = null,
    bool? IsActive = null) : IRequest<PagedResult<ProductDto>>, ICacheable
{
    public string CacheKey => $"products:list:{Page}:{PageSize}:{Search}:{CategoryId}:{IsActive}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(2);
}

public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto?>, ICacheable
{
    public string CacheKey => $"products:id:{Id}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(2);
}

/// <summary>
/// Returns product by exact barcode match. Useful for POS barcode scanning.
/// </summary>
public record GetProductByBarcodeQuery(string Barcode) : IRequest<ProductDto?>;

public record GetCategoriesQuery(
    Guid? ParentId = null,
    bool? IsActive = null) : IRequest<IReadOnlyList<CategoryDto>>, ICacheable
{
    public string CacheKey => $"products:categories:{ParentId}:{IsActive}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
}
