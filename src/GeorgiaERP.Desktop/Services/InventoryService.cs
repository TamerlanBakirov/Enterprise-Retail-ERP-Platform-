using GeorgiaERP.Desktop.Models;

namespace GeorgiaERP.Desktop.Services;

public interface IInventoryService
{
    Task<PagedResult<StockLevelDto>> GetStockLevelsAsync(Guid? warehouseId = null, Guid? productId = null, bool? lowStockOnly = null, int page = 1, int pageSize = 50);
    Task<PagedResult<StockMovementDto>> GetMovementsAsync(Guid? warehouseId = null, Guid? productId = null, int page = 1, int pageSize = 50);
    Task<ApiResult> AdjustStockAsync(AdjustStockRequest request);
    Task<PagedResult<TransferOrderDto>> GetTransfersAsync(Guid? warehouseId = null, string? status = null, int page = 1, int pageSize = 20);
    Task<ApiResult> ApproveTransferAsync(Guid id);
    Task<ApiResult> ShipTransferAsync(Guid id);
    Task<ApiResult> ReceiveTransferAsync(Guid id);
    Task<PagedResult<StockCountDto>> GetStockCountsAsync(Guid? warehouseId = null, string? status = null, int page = 1, int pageSize = 20);
}

public class InventoryService : IInventoryService
{
    private readonly IApiClient _api;
    public InventoryService(IApiClient api) => _api = api;

    public Task<PagedResult<StockLevelDto>> GetStockLevelsAsync(Guid? warehouseId, Guid? productId, bool? lowStockOnly, int page, int pageSize)
    {
        var q = $"inventory/stock-levels?page={page}&pageSize={pageSize}";
        if (warehouseId.HasValue) q += $"&warehouseId={warehouseId}";
        if (productId.HasValue) q += $"&productId={productId}";
        if (lowStockOnly == true) q += "&lowStockOnly=true";
        return _api.GetAsync<PagedResult<StockLevelDto>>(q)!;
    }

    public Task<PagedResult<StockMovementDto>> GetMovementsAsync(Guid? warehouseId, Guid? productId, int page, int pageSize)
    {
        var q = $"inventory/movements?page={page}&pageSize={pageSize}";
        if (warehouseId.HasValue) q += $"&warehouseId={warehouseId}";
        if (productId.HasValue) q += $"&productId={productId}";
        return _api.GetAsync<PagedResult<StockMovementDto>>(q)!;
    }

    public Task<ApiResult> AdjustStockAsync(AdjustStockRequest request) =>
        _api.PostAsync("inventory/adjust", request);

    public Task<PagedResult<TransferOrderDto>> GetTransfersAsync(Guid? warehouseId, string? status, int page, int pageSize)
    {
        var q = $"inventory/transfers?page={page}&pageSize={pageSize}";
        if (warehouseId.HasValue) q += $"&warehouseId={warehouseId}";
        if (!string.IsNullOrEmpty(status)) q += $"&status={status}";
        return _api.GetAsync<PagedResult<TransferOrderDto>>(q)!;
    }

    public Task<ApiResult> ApproveTransferAsync(Guid id) => _api.PostAsync($"inventory/transfers/{id}/approve");
    public Task<ApiResult> ShipTransferAsync(Guid id) => _api.PostAsync($"inventory/transfers/{id}/ship");
    public Task<ApiResult> ReceiveTransferAsync(Guid id) => _api.PostAsync($"inventory/transfers/{id}/receive");

    public Task<PagedResult<StockCountDto>> GetStockCountsAsync(Guid? warehouseId, string? status, int page, int pageSize)
    {
        var q = $"inventory/counts?page={page}&pageSize={pageSize}";
        if (warehouseId.HasValue) q += $"&warehouseId={warehouseId}";
        if (!string.IsNullOrEmpty(status)) q += $"&status={status}";
        return _api.GetAsync<PagedResult<StockCountDto>>(q)!;
    }
}
