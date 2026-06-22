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
    Task<TransferOrderDto?> CreateTransferAsync(CreateTransferOrderRequest request);
    Task<StockCountDto?> CreateStockCountAsync(CreateStockCountRequest request);
    Task<ApiResult> RecordCountLineAsync(Guid countId, Guid lineId, decimal countedQty);
    Task<ApiResult> CompleteStockCountAsync(Guid countId);
}

public class InventoryService : IInventoryService
{
    private readonly IApiClient _api;
    public InventoryService(IApiClient api) => _api = api;

    public async Task<PagedResult<StockLevelDto>> GetStockLevelsAsync(Guid? warehouseId, Guid? productId, bool? lowStockOnly, int page, int pageSize)
    {
        var q = $"inventory/stock-levels?page={page}&pageSize={pageSize}";
        if (warehouseId.HasValue) q += $"&warehouseId={warehouseId}";
        if (productId.HasValue) q += $"&productId={productId}";
        if (lowStockOnly == true) q += "&lowStockOnly=true";
        return await _api.GetAsync<PagedResult<StockLevelDto>>(q) ?? new PagedResult<StockLevelDto>();
    }

    public async Task<PagedResult<StockMovementDto>> GetMovementsAsync(Guid? warehouseId, Guid? productId, int page, int pageSize)
    {
        var q = $"inventory/movements?page={page}&pageSize={pageSize}";
        if (warehouseId.HasValue) q += $"&warehouseId={warehouseId}";
        if (productId.HasValue) q += $"&productId={productId}";
        return await _api.GetAsync<PagedResult<StockMovementDto>>(q) ?? new PagedResult<StockMovementDto>();
    }

    public Task<ApiResult> AdjustStockAsync(AdjustStockRequest request) =>
        _api.PostAsync("inventory/adjust", request);

    public async Task<PagedResult<TransferOrderDto>> GetTransfersAsync(Guid? warehouseId, string? status, int page, int pageSize)
    {
        var q = $"inventory/transfers?page={page}&pageSize={pageSize}";
        if (warehouseId.HasValue) q += $"&warehouseId={warehouseId}";
        if (!string.IsNullOrEmpty(status)) q += $"&status={status}";
        return await _api.GetAsync<PagedResult<TransferOrderDto>>(q) ?? new PagedResult<TransferOrderDto>();
    }

    public Task<ApiResult> ApproveTransferAsync(Guid id) => _api.PostAsync($"inventory/transfers/{id}/approve");
    public Task<ApiResult> ShipTransferAsync(Guid id) => _api.PostAsync($"inventory/transfers/{id}/ship");
    public Task<ApiResult> ReceiveTransferAsync(Guid id) => _api.PostAsync($"inventory/transfers/{id}/receive");

    public async Task<PagedResult<StockCountDto>> GetStockCountsAsync(Guid? warehouseId, string? status, int page, int pageSize)
    {
        var q = $"inventory/counts?page={page}&pageSize={pageSize}";
        if (warehouseId.HasValue) q += $"&warehouseId={warehouseId}";
        if (!string.IsNullOrEmpty(status)) q += $"&status={status}";
        return await _api.GetAsync<PagedResult<StockCountDto>>(q) ?? new PagedResult<StockCountDto>();
    }

    public Task<TransferOrderDto?> CreateTransferAsync(CreateTransferOrderRequest request) =>
        _api.PostAsync<CreateTransferOrderRequest, TransferOrderDto>("inventory/transfers", request);

    public Task<StockCountDto?> CreateStockCountAsync(CreateStockCountRequest request) =>
        _api.PostAsync<CreateStockCountRequest, StockCountDto>("inventory/counts", request);

    public Task<ApiResult> RecordCountLineAsync(Guid countId, Guid lineId, decimal countedQty) =>
        _api.PostAsync($"inventory/counts/{countId}/lines/{lineId}/record", new { countedQuantity = countedQty });

    public Task<ApiResult> CompleteStockCountAsync(Guid countId) =>
        _api.PostAsync($"inventory/counts/{countId}/complete");
}
