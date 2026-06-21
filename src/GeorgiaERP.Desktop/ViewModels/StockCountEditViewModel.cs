using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class StockCountEditViewModel : DialogViewModel
{
    private readonly IInventoryService _inventoryService;
    private readonly IOrganizationService _organizationService;

    [ObservableProperty] private Guid? _warehouseId;
    [ObservableProperty] private string _countType = "Full";

    public ObservableCollection<WarehouseDto> Warehouses { get; } = [];

    public StockCountEditViewModel(IInventoryService inventoryService, IOrganizationService organizationService)
    {
        _inventoryService = inventoryService;
        _organizationService = organizationService;
    }

    public async Task LoadDataAsync()
    {
        try
        {
            var warehouses = await _organizationService.GetWarehousesAsync(true);
            ReplaceItems(Warehouses, warehouses);
        }
        catch { }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (WarehouseId is null)
        {
            ErrorMessage = "Warehouse is required.";
            return;
        }

        ErrorMessage = null;
        try
        {
            var request = new CreateStockCountRequest(WarehouseId.Value, CountType, null);
            var result = await _inventoryService.CreateStockCountAsync(request);
            if (result is not null)
                SaveAndClose();
            else
                ErrorMessage = "Failed to create stock count.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
