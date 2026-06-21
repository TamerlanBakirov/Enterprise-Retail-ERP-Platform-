using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class WarehouseViewModel : TabbedPagedViewModel
{
    private readonly IWarehouseService _warehouseService;
    private readonly IOrganizationService _organizationService;

    [ObservableProperty] private WarehouseDto? _selectedWarehouse;
    [ObservableProperty] private string _statusFilter = string.Empty;
    [ObservableProperty] private ReceivingOrderDto? _selectedReceiving;
    [ObservableProperty] private ShippingOrderDto? _selectedShipping;

    public ObservableCollection<WarehouseDto> Warehouses { get; } = [];
    public ObservableCollection<WarehouseLocationDto> Locations { get; } = [];
    public ObservableCollection<ReceivingOrderDto> ReceivingOrders { get; } = [];
    public ObservableCollection<ShippingOrderDto> ShippingOrders { get; } = [];

    public WarehouseViewModel(IWarehouseService warehouseService, IOrganizationService organizationService)
    {
        _warehouseService = warehouseService;
        _organizationService = organizationService;
        ActiveTab = "Receiving";
    }

    protected override async Task LoadCoreAsync()
    {
        if (Warehouses.Count == 0)
        {
            var whs = await _organizationService.GetWarehousesAsync(true);
            ReplaceItems(Warehouses, whs);
        }

        switch (ActiveTab)
        {
            case "Receiving":
                var receiving = await _warehouseService.GetReceivingOrdersAsync(
                    SelectedWarehouse?.Id,
                    string.IsNullOrEmpty(StatusFilter) ? null : StatusFilter,
                    page: CurrentPage);
                ReplaceItems(ReceivingOrders, receiving.Items);
                TotalPages = receiving.TotalPages;
                TotalCount = receiving.TotalCount;
                break;

            case "Shipping":
                var shipping = await _warehouseService.GetShippingOrdersAsync(
                    SelectedWarehouse?.Id,
                    string.IsNullOrEmpty(StatusFilter) ? null : StatusFilter,
                    page: CurrentPage);
                ReplaceItems(ShippingOrders, shipping.Items);
                TotalPages = shipping.TotalPages;
                TotalCount = shipping.TotalCount;
                break;

            case "Locations":
                if (SelectedWarehouse is not null)
                {
                    var locs = await _warehouseService.GetLocationsAsync(SelectedWarehouse.Id);
                    ReplaceItems(Locations, locs);
                }
                else
                {
                    Locations.Clear();
                }
                break;
        }
    }

    [RelayCommand]
    private async Task StartReceivingAsync()
    {
        if (SelectedReceiving is null) return;
        await ExecuteAsync(async () =>
        {
            var result = await _warehouseService.StartReceivingAsync(SelectedReceiving.Id);
            if (!result.IsSuccess) { ErrorMessage = result.Error; return; }
            await LoadCoreAsync();
        });
    }

    [RelayCommand]
    private async Task CompleteReceivingAsync()
    {
        if (SelectedReceiving is null) return;
        await ExecuteAsync(async () =>
        {
            var result = await _warehouseService.CompleteReceivingAsync(SelectedReceiving.Id);
            if (!result.IsSuccess) { ErrorMessage = result.Error; return; }
            await LoadCoreAsync();
        });
    }

    [RelayCommand]
    private async Task CancelReceivingAsync()
    {
        if (SelectedReceiving is null) return;
        await ExecuteAsync(async () =>
        {
            var result = await _warehouseService.CancelReceivingAsync(SelectedReceiving.Id);
            if (!result.IsSuccess) { ErrorMessage = result.Error; return; }
            await LoadCoreAsync();
        });
    }

    [RelayCommand]
    private async Task StartPickingAsync()
    {
        if (SelectedShipping is null) return;
        await ExecuteAsync(async () =>
        {
            var result = await _warehouseService.StartPickingAsync(SelectedShipping.Id);
            if (!result.IsSuccess) { ErrorMessage = result.Error; return; }
            await LoadCoreAsync();
        });
    }

    [RelayCommand]
    private async Task PackOrderAsync()
    {
        if (SelectedShipping is null) return;
        await ExecuteAsync(async () =>
        {
            var result = await _warehouseService.PackOrderAsync(SelectedShipping.Id);
            if (!result.IsSuccess) { ErrorMessage = result.Error; return; }
            await LoadCoreAsync();
        });
    }

    [RelayCommand]
    private async Task ShipOrderAsync()
    {
        if (SelectedShipping is null) return;
        await ExecuteAsync(async () =>
        {
            var result = await _warehouseService.ShipOrderAsync(SelectedShipping.Id);
            if (!result.IsSuccess) { ErrorMessage = result.Error; return; }
            await LoadCoreAsync();
        });
    }

    [RelayCommand]
    private async Task CancelShippingAsync()
    {
        if (SelectedShipping is null) return;
        await ExecuteAsync(async () =>
        {
            var result = await _warehouseService.CancelShippingAsync(SelectedShipping.Id);
            if (!result.IsSuccess) { ErrorMessage = result.Error; return; }
            await LoadCoreAsync();
        });
    }
}
