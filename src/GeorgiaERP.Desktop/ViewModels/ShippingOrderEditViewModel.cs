using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class ShippingOrderEditViewModel : DialogViewModel
{
    private readonly IWarehouseService _warehouseService;

    [ObservableProperty] private Guid? _warehouseId;
    [ObservableProperty] private string _orderType = "SalesOrder";
    [ObservableProperty] private Guid? _destWarehouseId;
    [ObservableProperty] private string? _shippingAddress;
    [ObservableProperty] private string? _carrier;
    [ObservableProperty] private DateTimeOffset? _expectedShipDate;
    [ObservableProperty] private string? _notes;

    public ObservableCollection<WarehouseDto> Warehouses { get; } = [];
    public ObservableCollection<ProductDto> Products { get; } = [];
    public ObservableCollection<ShippingLineEditViewModel> Lines { get; } = [];

    public ShippingOrderEditViewModel(IWarehouseService warehouseService)
    {
        _warehouseService = warehouseService;
        Lines.Add(new ShippingLineEditViewModel());
    }

    public void SetData(IEnumerable<WarehouseDto> warehouses, IEnumerable<ProductDto> products)
    {
        ReplaceItems(Warehouses, warehouses);
        ReplaceItems(Products, products);
    }

    [RelayCommand]
    private void AddLine() => Lines.Add(new ShippingLineEditViewModel());

    [RelayCommand]
    private void RemoveLine(ShippingLineEditViewModel line)
    {
        if (Lines.Count > 1) Lines.Remove(line);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (WarehouseId is null)
        {
            ErrorMessage = "Warehouse is required.";
            return;
        }
        if (Lines.Any(l => l.ProductId is null || l.OrderedQty <= 0))
        {
            ErrorMessage = "All lines must have a product and ordered quantity > 0.";
            return;
        }

        ErrorMessage = null;
        try
        {
            var lines = Lines.Select(l => new ShippingLineInput(l.ProductId!.Value, l.OrderedQty)).ToList();
            var request = new CreateShippingOrderRequest(WarehouseId.Value, OrderType, null, null, DestWarehouseId, ShippingAddress, Carrier, ExpectedShipDate, Notes, lines);
            var result = await _warehouseService.CreateShippingOrderAsync(request);
            if (result is not null) SaveAndClose();
            else ErrorMessage = "Failed to create shipping order.";
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
    }
}

public partial class ShippingLineEditViewModel : ObservableObject
{
    [ObservableProperty] private Guid? _productId;
    [ObservableProperty] private decimal _orderedQty = 1;
}
