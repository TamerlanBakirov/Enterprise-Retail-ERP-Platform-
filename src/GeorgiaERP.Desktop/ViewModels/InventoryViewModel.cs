using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class InventoryViewModel : ObservableObject
{
    private readonly IInventoryService _inventoryService;
    private readonly IOrganizationService _organizationService;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private string _activeTab = "StockLevels";
    [ObservableProperty] private WarehouseDto? _selectedWarehouse;
    [ObservableProperty] private bool _lowStockOnly;

    public ObservableCollection<StockLevelDto> StockLevels { get; } = [];
    public ObservableCollection<StockMovementDto> Movements { get; } = [];
    public ObservableCollection<TransferOrderDto> Transfers { get; } = [];
    public ObservableCollection<WarehouseDto> Warehouses { get; } = [];

    public InventoryViewModel(IInventoryService inventoryService, IOrganizationService organizationService)
    {
        _inventoryService = inventoryService;
        _organizationService = organizationService;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            if (Warehouses.Count == 0)
            {
                var whs = await _organizationService.GetWarehousesAsync(true);
                foreach (var w in whs) Warehouses.Add(w);
            }

            switch (ActiveTab)
            {
                case "StockLevels":
                    var levels = await _inventoryService.GetStockLevelsAsync(
                        SelectedWarehouse?.Id, lowStockOnly: LowStockOnly, page: CurrentPage);
                    StockLevels.Clear();
                    foreach (var l in levels.Items) StockLevels.Add(l);
                    TotalPages = levels.TotalPages;
                    break;

                case "Movements":
                    var moves = await _inventoryService.GetMovementsAsync(
                        SelectedWarehouse?.Id, page: CurrentPage);
                    Movements.Clear();
                    foreach (var m in moves.Items) Movements.Add(m);
                    TotalPages = moves.TotalPages;
                    break;

                case "Transfers":
                    var transfers = await _inventoryService.GetTransfersAsync(
                        SelectedWarehouse?.Id, page: CurrentPage);
                    Transfers.Clear();
                    foreach (var t in transfers.Items) Transfers.Add(t);
                    TotalPages = transfers.TotalPages;
                    break;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SwitchTabAsync(string tab)
    {
        ActiveTab = tab;
        CurrentPage = 1;
        await LoadAsync();
    }
}
