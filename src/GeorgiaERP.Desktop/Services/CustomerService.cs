using GeorgiaERP.Desktop.Models;

namespace GeorgiaERP.Desktop.Services;

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> GetCustomersAsync(string? search = null, bool? isActive = null, int page = 1, int pageSize = 20);
    Task<CustomerDto?> CreateCustomerAsync(CreateCustomerRequest request);
    Task<ApiResult> EarnPointsAsync(Guid customerId, EarnPointsRequest request);
    Task<ApiResult> RedeemPointsAsync(Guid customerId, RedeemPointsRequest request);
    Task<PagedResult<LoyaltyTransactionDto>> GetLoyaltyHistoryAsync(Guid customerId, int page = 1, int pageSize = 20);
    Task<ApiResult> ExpireLoyaltyPointsAsync(int inactivityMonths = 12);
    Task<ApiResult> RecalculateLoyaltyTiersAsync();
}

public class CustomerService : ICustomerService
{
    private readonly IApiClient _api;
    public CustomerService(IApiClient api) => _api = api;

    public async Task<PagedResult<CustomerDto>> GetCustomersAsync(string? search, bool? isActive, int page, int pageSize)
    {
        var q = $"customers?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(search)) q += $"&search={Uri.EscapeDataString(search)}";
        if (isActive.HasValue) q += $"&isActive={isActive}";
        return await _api.GetAsync<PagedResult<CustomerDto>>(q) ?? new PagedResult<CustomerDto>();
    }

    public Task<CustomerDto?> CreateCustomerAsync(CreateCustomerRequest request) =>
        _api.PostAsync<CreateCustomerRequest, CustomerDto>("customers", request);

    public Task<ApiResult> EarnPointsAsync(Guid customerId, EarnPointsRequest request) =>
        _api.PostAsync($"customers/{customerId}/loyalty/earn", request);

    public Task<ApiResult> RedeemPointsAsync(Guid customerId, RedeemPointsRequest request) =>
        _api.PostAsync($"customers/{customerId}/loyalty/redeem", request);

    public async Task<PagedResult<LoyaltyTransactionDto>> GetLoyaltyHistoryAsync(Guid customerId, int page, int pageSize) =>
        await _api.GetAsync<PagedResult<LoyaltyTransactionDto>>(
            $"customers/{customerId}/loyalty/transactions?page={page}&pageSize={pageSize}")
        ?? new PagedResult<LoyaltyTransactionDto>();

    public Task<ApiResult> ExpireLoyaltyPointsAsync(int inactivityMonths) =>
        _api.PostAsync($"customers/loyalty/expire?inactivityMonths={inactivityMonths}");

    public Task<ApiResult> RecalculateLoyaltyTiersAsync() =>
        _api.PostAsync("customers/loyalty/recalculate-tiers");
}
