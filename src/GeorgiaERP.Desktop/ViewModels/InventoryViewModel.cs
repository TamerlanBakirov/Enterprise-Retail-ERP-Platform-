using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class InventoryViewModel : TabbedPagedViewModel
{
    private readonly IInventoryService _inventoryService;
    private readonly IOrganizationService _organizationService;

    [ObservableProperty] private WarehouseDto? _selectedWarehouse;
    [ObservableProperty] private bool _lowStockOnly;

    public ObservableCollection<StockLevelDto> StockLevels { get; } = [];
    public ObservableCollection<StockMovementDto> Movements { get; } = [];
    public ObservableCollection<TransferOrderDto> Transfers { get; } = [];
    public ObservableCollection<StockCountDto> StockCounts { get; } = [];
    public ObservableCollection<WarehouseDto> Warehouses { get; } = [];

    public InventoryViewModel(IInventoryService inventoryService, IOrganizationService organizationService)
    {
        _inventoryService = inventoryService;
        _organizationService = organizationService;
        ActiveTab = "StockLevels";
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
            case "StockLevels":
                var levels = await _inventoryService.GetStockLevelsAsync(
                    SelectedWarehouse?.Id, lowStockOnly: LowStockOnly, page: CurrentPage);
                ReplaceItems(StockLevels, levels.Items);
                TotalPages = levels.TotalPages;
                break;

            case "Movements":
                var moves = await _inventoryService.GetMovementsAsync(
                    SelectedWarehouse?.Id, page: CurrentPage);
                ReplaceItems(Movements, moves.Items);
                TotalPages = moves.TotalPages;
                break;

            case "Transfers":
                var transfers = await _inventoryService.GetTransfersAsync(
                    SelectedWarehouse?.Id, page: CurrentPage);
                ReplaceItems(Transfers, transfers.Items);
                TotalPages = transfers.TotalPages;
                break;

            case "StockCounts":
                var counts = await _inventoryService.GetStockCountsAsync(
                    SelectedWarehouse?.Id, page: CurrentPage);
                ReplaceItems(StockCounts, counts.Items);
                TotalPages = counts.TotalPages;
                break;
        }
    }

    [RelayCommand]
    private async Task CompleteStockCountAsync(StockCountDto count)
    {
        var result = await _inventoryService.CompleteStockCountAsync(count.Id);
        if (result.IsSuccess) await LoadAsync();
        else ErrorMessage = result.Error;
    }
}
