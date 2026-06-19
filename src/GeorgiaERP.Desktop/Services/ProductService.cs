using GeorgiaERP.Desktop.Models;

namespace GeorgiaERP.Desktop.Services;

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetProductsAsync(string? search = null, Guid? categoryId = null, bool? isActive = null, int page = 1, int pageSize = 20);
    Task<ProductDto?> GetProductAsync(Guid id);
    Task<ProductDto?> CreateProductAsync(CreateProductRequest request);
    Task<List<CategoryDto>> GetCategoriesAsync(Guid? parentId = null);
}

public class ProductService : IProductService
{
    private readonly IApiClient _api;
    public ProductService(IApiClient api) => _api = api;

    public Task<PagedResult<ProductDto>> GetProductsAsync(string? search, Guid? categoryId, bool? isActive, int page, int pageSize)
    {
        var query = $"products?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(search)) query += $"&search={Uri.EscapeDataString(search)}";
        if (categoryId.HasValue) query += $"&categoryId={categoryId}";
        if (isActive.HasValue) query += $"&isActive={isActive}";
        return _api.GetAsync<PagedResult<ProductDto>>(query)!;
    }

    public Task<ProductDto?> GetProductAsync(Guid id) => _api.GetAsync<ProductDto>($"products/{id}");

    public Task<ProductDto?> CreateProductAsync(CreateProductRequest request) =>
        _api.PostAsync<CreateProductRequest, ProductDto>("products", request);

    public Task<List<CategoryDto>> GetCategoriesAsync(Guid? parentId)
    {
        var query = "products/categories";
        if (parentId.HasValue) query += $"?parentId={parentId}";
        return _api.GetAsync<List<CategoryDto>>(query)!;
    }
}
