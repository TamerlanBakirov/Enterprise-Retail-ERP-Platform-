using GeorgiaERP.Desktop.Models;

namespace GeorgiaERP.Desktop.Services;

public interface IWarehouseService
{
    Task<WarehouseDetailDto?> GetWarehouseAsync(Guid id);
    Task<List<WarehouseLocationDto>> GetLocationsAsync(Guid warehouseId, string? locationType = null, bool? isActive = null);
    Task<PagedResult<ReceivingOrderDto>> GetReceivingOrdersAsync(Guid? warehouseId = null, string? status = null, int page = 1, int pageSize = 20);
    Task<PagedResult<ShippingOrderDto>> GetShippingOrdersAsync(Guid? warehouseId = null, string? status = null, int page = 1, int pageSize = 20);
    Task<ApiResult> StartReceivingAsync(Guid orderId);
    Task<ApiResult> CompleteReceivingAsync(Guid orderId);
    Task<ApiResult> CancelReceivingAsync(Guid orderId);
    Task<ApiResult> StartPickingAsync(Guid orderId);
    Task<ApiResult> PackOrderAsync(Guid orderId);
    Task<ApiResult> ShipOrderAsync(Guid orderId, string? trackingNumber = null);
    Task<ApiResult> CancelShippingAsync(Guid orderId);
    Task<WarehouseDetailDto?> CreateWarehouseAsync(CreateWarehouseRequest request);
    Task<WarehouseLocationDto?> CreateLocationAsync(Guid warehouseId, CreateLocationRequest request);
    Task<ReceivingOrderDto?> CreateReceivingOrderAsync(CreateReceivingOrderRequest request);
    Task<ShippingOrderDto?> CreateShippingOrderAsync(CreateShippingOrderRequest request);
}

public class WarehouseService : IWarehouseService
{
    private readonly IApiClient _api;
    public WarehouseService(IApiClient api) => _api = api;

    public Task<WarehouseDetailDto?> GetWarehouseAsync(Guid id) =>
        _api.GetAsync<WarehouseDetailDto>($"warehouse/{id}");

    public async Task<List<WarehouseLocationDto>> GetLocationsAsync(Guid warehouseId, string? locationType, bool? isActive)
    {
        var q = $"warehouse/{warehouseId}/locations";
        var sep = '?';
        if (locationType is not null) { q += $"{sep}locationType={locationType}"; sep = '&'; }
        if (isActive.HasValue) { q += $"{sep}isActive={isActive.Value}"; }
        return await _api.GetAsync<List<WarehouseLocationDto>>(q) ?? [];
    }

    public async Task<PagedResult<ReceivingOrderDto>> GetReceivingOrdersAsync(Guid? warehouseId, string? status, int page, int pageSize)
    {
        var q = $"warehouse/receiving?page={page}&pageSize={pageSize}";
        if (warehouseId.HasValue) q += $"&warehouseId={warehouseId}";
        if (!string.IsNullOrEmpty(status)) q += $"&status={status}";
        return await _api.GetAsync<PagedResult<ReceivingOrderDto>>(q) ?? new PagedResult<ReceivingOrderDto>();
    }

    public async Task<PagedResult<ShippingOrderDto>> GetShippingOrdersAsync(Guid? warehouseId, string? status, int page, int pageSize)
    {
        var q = $"warehouse/shipping?page={page}&pageSize={pageSize}";
        if (warehouseId.HasValue) q += $"&warehouseId={warehouseId}";
        if (!string.IsNullOrEmpty(status)) q += $"&status={status}";
        return await _api.GetAsync<PagedResult<ShippingOrderDto>>(q) ?? new PagedResult<ShippingOrderDto>();
    }

    public Task<ApiResult> StartReceivingAsync(Guid orderId) =>
        _api.PostAsync($"warehouse/receiving/{orderId}/start");

    public Task<ApiResult> CompleteReceivingAsync(Guid orderId) =>
        _api.PostAsync($"warehouse/receiving/{orderId}/complete");

    public Task<ApiResult> CancelReceivingAsync(Guid orderId) =>
        _api.PostAsync($"warehouse/receiving/{orderId}/cancel");

    public Task<ApiResult> StartPickingAsync(Guid orderId) =>
        _api.PostAsync($"warehouse/shipping/{orderId}/pick");

    public Task<ApiResult> PackOrderAsync(Guid orderId) =>
        _api.PostAsync($"warehouse/shipping/{orderId}/pack");

    public Task<ApiResult> ShipOrderAsync(Guid orderId, string? trackingNumber)
    {
        var body = new { trackingNumber };
        return _api.PostAsync($"warehouse/shipping/{orderId}/ship", body);
    }

    public Task<ApiResult> CancelShippingAsync(Guid orderId) =>
        _api.PostAsync($"warehouse/shipping/{orderId}/cancel");

    public Task<WarehouseDetailDto?> CreateWarehouseAsync(CreateWarehouseRequest request) =>
        _api.PostAsync<CreateWarehouseRequest, WarehouseDetailDto>("warehouse", request);

    public Task<WarehouseLocationDto?> CreateLocationAsync(Guid warehouseId, CreateLocationRequest request) =>
        _api.PostAsync<CreateLocationRequest, WarehouseLocationDto>($"warehouse/{warehouseId}/locations", request);

    public Task<ReceivingOrderDto?> CreateReceivingOrderAsync(CreateReceivingOrderRequest request) =>
        _api.PostAsync<CreateReceivingOrderRequest, ReceivingOrderDto>("warehouse/receiving", request);

    public Task<ShippingOrderDto?> CreateShippingOrderAsync(CreateShippingOrderRequest request) =>
        _api.PostAsync<CreateShippingOrderRequest, ShippingOrderDto>("warehouse/shipping", request);
}
