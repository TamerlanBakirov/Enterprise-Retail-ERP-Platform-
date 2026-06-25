using GeorgiaERP.Desktop.Models;

namespace GeorgiaERP.Desktop.Services;

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetProductsAsync(string? search = null, Guid? categoryId = null, bool? isActive = null, int page = 1, int pageSize = 20);
    Task<ProductDto?> GetProductAsync(Guid id);
    Task<ProductDto?> CreateProductAsync(CreateProductRequest request);
    Task<List<CategoryDto>> GetCategoriesAsync(Guid? parentId = null);
    Task<ApiResult> UpdateProductAsync(Guid id, UpdateProductRequest request);
    Task<ApiResult> DeleteProductAsync(Guid id);

    /// <summary>
    /// Sets the product's retail price on the active Retail price list.
    /// Returns false when no Retail price list exists.
    /// </summary>
    Task<bool> SetRetailPriceAsync(Guid productId, decimal price);
}

public class ProductService : IProductService
{
    private readonly IApiClient _api;
    private readonly IPricingService _pricing;
    public ProductService(IApiClient api, IPricingService pricing)
    {
        _api = api;
        _pricing = pricing;
    }

    public async Task<PagedResult<ProductDto>> GetProductsAsync(string? search, Guid? categoryId, bool? isActive, int page, int pageSize)
    {
        var query = $"products?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(search)) query += $"&search={Uri.EscapeDataString(search)}";
        if (categoryId.HasValue) query += $"&categoryId={categoryId}";
        if (isActive.HasValue) query += $"&isActive={isActive}";
        return await _api.GetAsync<PagedResult<ProductDto>>(query) ?? new PagedResult<ProductDto>();
    }

    public Task<ProductDto?> GetProductAsync(Guid id) => _api.GetAsync<ProductDto>($"products/{id}");

    public Task<ProductDto?> CreateProductAsync(CreateProductRequest request) =>
        _api.PostAsync<CreateProductRequest, ProductDto>("products", request);

    public async Task<List<CategoryDto>> GetCategoriesAsync(Guid? parentId)
    {
        var query = "products/categories";
        if (parentId.HasValue) query += $"?parentId={parentId}";
        return await _api.GetAsync<List<CategoryDto>>(query) ?? [];
    }

    public Task<ApiResult> UpdateProductAsync(Guid id, UpdateProductRequest request) =>
        _api.PutAsync("products/" + id, request);

    public Task<ApiResult> DeleteProductAsync(Guid id) =>
        _api.DeleteAsync("products/" + id);

    public async Task<bool> SetRetailPriceAsync(Guid productId, decimal price)
    {
        var lists = await _pricing.GetPriceListsAsync(priceType: "Retail", isActive: true, page: 1, pageSize: 1);
        var priceList = lists.Items.FirstOrDefault();
        if (priceList is null)
            return false;

        var result = await _pricing.SetPriceAsync(
            new SetPriceRequest(priceList.Id, productId, price, MinQty: 1, VariantId: null));
        return result is not null;
    }
}
