using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class WarehouseEditViewModel : DialogViewModel
{
    private readonly IWarehouseService _warehouseService;

    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _nameKa;
    [ObservableProperty] private string _warehouseType = "Main";
    [ObservableProperty] private string? _address;
    [ObservableProperty] private string? _city;
    [ObservableProperty] private string? _region;
    [ObservableProperty] private Guid? _linkedStoreId;

    public ObservableCollection<StoreDto> Stores { get; } = [];

    public WarehouseEditViewModel(IWarehouseService warehouseService, IOrganizationService organizationService)
    {
        _warehouseService = warehouseService;
        _ = LoadStoresAsync(organizationService);
    }

    private async Task LoadStoresAsync(IOrganizationService org)
    {
        try { ReplaceItems(Stores, await org.GetStoresAsync(true)); } catch { }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Code) || string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Code and Name are required.";
            return;
        }

        ErrorMessage = null;
        try
        {
            var request = new CreateWarehouseRequest(Code, Name, NameKa, WarehouseType, Address, City, Region, LinkedStoreId);
            var result = await _warehouseService.CreateWarehouseAsync(request);
            if (result is not null) SaveAndClose();
            else ErrorMessage = "Failed to create warehouse.";
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
    }
}
