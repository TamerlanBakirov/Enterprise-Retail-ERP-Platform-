using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class ProcurementViewModel : TabbedPagedViewModel
{
    private readonly IProcurementService _procurementService;

    public ObservableCollection<SupplierDto> Suppliers { get; } = [];
    public ObservableCollection<PurchaseOrderDto> PurchaseOrders { get; } = [];

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
}
