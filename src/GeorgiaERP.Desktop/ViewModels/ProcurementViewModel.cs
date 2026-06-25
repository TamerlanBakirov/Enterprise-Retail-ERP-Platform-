using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class ProcurementViewModel : TabbedPagedViewModel
{
    private readonly IProcurementService _procurementService;

    [ObservableProperty] private PurchaseOrderDto? _selectedPurchaseOrder;

    public ObservableCollection<SupplierDto> Suppliers { get; } = [];
    public ObservableCollection<PurchaseOrderDto> PurchaseOrders { get; } = [];
    public ObservableCollection<PurchaseOrderLineDto> SelectedPoLines { get; } = [];

    public ProcurementViewModel(IProcurementService procurementService)
    {
        _procurementService = procurementService;
        ActiveTab = "Suppliers";
    }

    protected override async Task LoadCoreAsync()
    {
        if (ActiveTab == "Suppliers")
        {
            var result = await _procurementService.GetSuppliersAsync(page: CurrentPage);
            ReplaceItems(Suppliers, result.Items);
            TotalPages = result.TotalPages;
        }
        else
        {
            var result = await _procurementService.GetPurchaseOrdersAsync(page: CurrentPage);
            ReplaceItems(PurchaseOrders, result.Items);
            TotalPages = result.TotalPages;
        }
    }

    [RelayCommand]
    private async Task ApprovePurchaseOrderAsync(PurchaseOrderDto po)
    {
        var result = await _procurementService.ApprovePurchaseOrderAsync(po.Id);
        if (result.IsSuccess) await LoadAsync();
        else ErrorMessage = result.Error;
    }

    // Loads the selected order's lines (with product names) into the detail panel.
    partial void OnSelectedPurchaseOrderChanged(PurchaseOrderDto? value)
    {
        SelectedPoLines.Clear();
        if (value is null) return;
        _ = LoadPurchaseOrderLinesAsync(value.Id);
    }

    private async Task LoadPurchaseOrderLinesAsync(Guid id)
    {
        var detail = await _procurementService.GetPurchaseOrderByIdAsync(id);
        if (SelectedPurchaseOrder?.Id != id || detail?.Lines is null) return;
        ReplaceItems(SelectedPoLines, detail.Lines);
    }
}
