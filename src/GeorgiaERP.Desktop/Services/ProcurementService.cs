using GeorgiaERP.Desktop.Models;

namespace GeorgiaERP.Desktop.Services;

public interface IProcurementService
{
    Task<PagedResult<SupplierDto>> GetSuppliersAsync(string? search = null, bool? isActive = null, int page = 1, int pageSize = 20);
    Task<SupplierDto?> CreateSupplierAsync(CreateSupplierRequest request);
    Task<PagedResult<PurchaseOrderDto>> GetPurchaseOrdersAsync(Guid? supplierId = null, string? status = null, int page = 1, int pageSize = 20);
    Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(Guid id);
    Task<PurchaseOrderDto?> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request);
    Task<ApiResult> ApprovePurchaseOrderAsync(Guid id);
    Task<ApiResult> SendPurchaseOrderAsync(Guid id);
    Task<ApiResult> CancelPurchaseOrderAsync(Guid id);
    Task<ApiResult> ReceiveGoodsAsync(ReceiveGoodsRequest request);
}

public class ProcurementService : IProcurementService
{
    private readonly IApiClient _api;
    public ProcurementService(IApiClient api) => _api = api;

    public async Task<PagedResult<SupplierDto>> GetSuppliersAsync(string? search, bool? isActive, int page, int pageSize)
    {
        var q = $"procurement/suppliers?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(search)) q += $"&search={Uri.EscapeDataString(search)}";
        if (isActive.HasValue) q += $"&isActive={isActive}";
        return await _api.GetAsync<PagedResult<SupplierDto>>(q) ?? new PagedResult<SupplierDto>();
    }

    public Task<SupplierDto?> CreateSupplierAsync(CreateSupplierRequest request) =>
        _api.PostAsync<CreateSupplierRequest, SupplierDto>("procurement/suppliers", request);

    public async Task<PagedResult<PurchaseOrderDto>> GetPurchaseOrdersAsync(Guid? supplierId, string? status, int page, int pageSize)
    {
        var q = $"procurement/purchase-orders?page={page}&pageSize={pageSize}";
        if (supplierId.HasValue) q += $"&supplierId={supplierId}";
        if (!string.IsNullOrEmpty(status)) q += $"&status={status}";
        return await _api.GetAsync<PagedResult<PurchaseOrderDto>>(q) ?? new PagedResult<PurchaseOrderDto>();
    }

    public Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(Guid id) =>
        _api.GetAsync<PurchaseOrderDto>($"procurement/purchase-orders/{id}");

    public Task<PurchaseOrderDto?> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request) =>
        _api.PostAsync<CreatePurchaseOrderRequest, PurchaseOrderDto>("procurement/purchase-orders", request);

    public Task<ApiResult> ApprovePurchaseOrderAsync(Guid id) => _api.PostAsync($"procurement/purchase-orders/{id}/approve");
    public Task<ApiResult> SendPurchaseOrderAsync(Guid id) => _api.PostAsync($"procurement/purchase-orders/{id}/send");
    public Task<ApiResult> CancelPurchaseOrderAsync(Guid id) => _api.PostAsync($"procurement/purchase-orders/{id}/cancel");

    public Task<ApiResult> ReceiveGoodsAsync(ReceiveGoodsRequest request) =>
        _api.PostAsync($"procurement/purchase-orders/{request.PurchaseOrderId}/receive", request);
}
