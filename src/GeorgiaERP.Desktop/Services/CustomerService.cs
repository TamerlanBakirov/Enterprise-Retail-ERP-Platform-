using GeorgiaERP.Desktop.Models;

namespace GeorgiaERP.Desktop.Services;

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> GetCustomersAsync(string? search = null, bool? isActive = null, int page = 1, int pageSize = 20);
    Task<CustomerDto?> CreateCustomerAsync(CreateCustomerRequest request);
    Task<ApiResult> EarnPointsAsync(Guid customerId, EarnPointsRequest request);
    Task<ApiResult> RedeemPointsAsync(Guid customerId, RedeemPointsRequest request);
}

public class CustomerService : ICustomerService
{
    private readonly IApiClient _api;
    public CustomerService(IApiClient api) => _api = api;

    public Task<PagedResult<CustomerDto>> GetCustomersAsync(string? search, bool? isActive, int page, int pageSize)
    {
        var q = $"customers?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(search)) q += $"&search={Uri.EscapeDataString(search)}";
        if (isActive.HasValue) q += $"&isActive={isActive}";
        return _api.GetAsync<PagedResult<CustomerDto>>(q)!;
    }

    public Task<CustomerDto?> CreateCustomerAsync(CreateCustomerRequest request) =>
        _api.PostAsync<CreateCustomerRequest, CustomerDto>("customers", request);

    public Task<ApiResult> EarnPointsAsync(Guid customerId, EarnPointsRequest request) =>
        _api.PostAsync($"customers/{customerId}/loyalty/earn", request);

    public Task<ApiResult> RedeemPointsAsync(Guid customerId, RedeemPointsRequest request) =>
        _api.PostAsync($"customers/{customerId}/loyalty/redeem", request);
}
