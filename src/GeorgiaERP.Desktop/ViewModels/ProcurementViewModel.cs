using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class ProcurementViewModel : ObservableObject
{
    private readonly IProcurementService _procurementService;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private string _activeTab = "Suppliers";

    public ObservableCollection<SupplierDto> Suppliers { get; } = [];
    public ObservableCollection<PurchaseOrderDto> PurchaseOrders { get; } = [];

    public ProcurementViewModel(IProcurementService procurementService)
    {
        _procurementService = procurementService;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            if (ActiveTab == "Suppliers")
            {
                var result = await _procurementService.GetSuppliersAsync(page: CurrentPage);
                Suppliers.Clear();
                foreach (var s in result.Items) Suppliers.Add(s);
                TotalPages = result.TotalPages;
            }
            else
            {
                var result = await _procurementService.GetPurchaseOrdersAsync(page: CurrentPage);
                PurchaseOrders.Clear();
                foreach (var po in result.Items) PurchaseOrders.Add(po);
                TotalPages = result.TotalPages;
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

    [RelayCommand]
    private async Task ApprovePurchaseOrderAsync(PurchaseOrderDto po)
    {
        var result = await _procurementService.ApprovePurchaseOrderAsync(po.Id);
        if (result.IsSuccess) await LoadAsync();
        else ErrorMessage = result.Error;
    }
}
