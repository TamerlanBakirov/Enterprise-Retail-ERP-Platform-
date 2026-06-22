using GeorgiaERP.Desktop.Models;

namespace GeorgiaERP.Desktop.Services;

public interface IPricingService
{
    Task<PagedResult<PriceListDto>> GetPriceListsAsync(string? priceType = null, bool? isActive = null, int page = 1, int pageSize = 20);
    Task<PriceListDto?> CreatePriceListAsync(CreatePriceListRequest request);
    Task<PagedResult<PriceListItemDto>> GetPriceListItemsAsync(Guid priceListId, string? search = null, int page = 1, int pageSize = 20);
    Task<PriceListItemDto?> SetPriceAsync(SetPriceRequest request);
    Task<PagedResult<PromotionDto>> GetPromotionsAsync(bool? isActive = null, int page = 1, int pageSize = 20);
    Task<PromotionDto?> CreatePromotionAsync(CreatePromotionRequest request);
}

public class PricingService : IPricingService
{
    private readonly IApiClient _api;
    public PricingService(IApiClient api) => _api = api;

    public async Task<PagedResult<PriceListDto>> GetPriceListsAsync(string? priceType, bool? isActive, int page, int pageSize)
    {
        var q = $"pricing/price-lists?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(priceType)) q += $"&priceType={priceType}";
        if (isActive.HasValue) q += $"&isActive={isActive}";
        return await _api.GetAsync<PagedResult<PriceListDto>>(q) ?? new PagedResult<PriceListDto>();
    }

    public Task<PriceListDto?> CreatePriceListAsync(CreatePriceListRequest request) =>
        _api.PostAsync<CreatePriceListRequest, PriceListDto>("pricing/price-lists", request);

    public async Task<PagedResult<PriceListItemDto>> GetPriceListItemsAsync(Guid priceListId, string? search, int page, int pageSize)
    {
        var q = $"pricing/price-lists/{priceListId}/items?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(search)) q += $"&search={Uri.EscapeDataString(search)}";
        return await _api.GetAsync<PagedResult<PriceListItemDto>>(q) ?? new PagedResult<PriceListItemDto>();
    }

    public Task<PriceListItemDto?> SetPriceAsync(SetPriceRequest request) =>
        _api.PostAsync<SetPriceRequest, PriceListItemDto>("pricing/prices", request);

    public async Task<PagedResult<PromotionDto>> GetPromotionsAsync(bool? isActive, int page, int pageSize)
    {
        var q = $"pricing/promotions?page={page}&pageSize={pageSize}";
        if (isActive.HasValue) q += $"&isActive={isActive}";
        return await _api.GetAsync<PagedResult<PromotionDto>>(q) ?? new PagedResult<PromotionDto>();
    }

    public Task<PromotionDto?> CreatePromotionAsync(CreatePromotionRequest request) =>
        _api.PostAsync<CreatePromotionRequest, PromotionDto>("pricing/promotions", request);
}
