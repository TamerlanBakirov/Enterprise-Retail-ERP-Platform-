using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class LocationEditViewModel : DialogViewModel
{
    private readonly IWarehouseService _warehouseService;
    private Guid _warehouseId;

    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _nameKa;
    [ObservableProperty] private string _locationType = "Shelf";
    [ObservableProperty] private int _sortOrder;
    [ObservableProperty] private int? _maxCapacity;
    [ObservableProperty] private string? _notes;

    public LocationEditViewModel(IWarehouseService warehouseService)
    {
        _warehouseService = warehouseService;
    }

    public void SetWarehouse(Guid warehouseId) => _warehouseId = warehouseId;

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
            var request = new CreateLocationRequest(Code, Name, NameKa, LocationType, null, SortOrder, MaxCapacity, Notes);
            var result = await _warehouseService.CreateLocationAsync(_warehouseId, request);
            if (result is not null) SaveAndClose();
            else ErrorMessage = "Failed to create location.";
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
    }
}
