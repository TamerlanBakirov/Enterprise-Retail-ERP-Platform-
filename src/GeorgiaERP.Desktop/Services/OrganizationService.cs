using GeorgiaERP.Desktop.Models;

namespace GeorgiaERP.Desktop.Services;

public interface IOrganizationService
{
    Task<List<StoreDto>> GetStoresAsync(bool? isActive = null);
    Task<List<WarehouseDto>> GetWarehousesAsync(bool? isActive = null);
}

public class OrganizationService : IOrganizationService
{
    private readonly IApiClient _api;
    public OrganizationService(IApiClient api) => _api = api;

    public Task<List<StoreDto>> GetStoresAsync(bool? isActive)
    {
        var q = "organization/stores";
        if (isActive.HasValue) q += $"?isActive={isActive}";
        return _api.GetAsync<List<StoreDto>>(q)!;
    }

    public Task<List<WarehouseDto>> GetWarehousesAsync(bool? isActive)
    {
        var q = "organization/warehouses";
        if (isActive.HasValue) q += $"?isActive={isActive}";
        return _api.GetAsync<List<WarehouseDto>>(q)!;
    }
}
