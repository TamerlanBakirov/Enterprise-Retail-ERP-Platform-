using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class ReceivingOrderEditViewModel : DialogViewModel
{
    private readonly IWarehouseService _warehouseService;

    [ObservableProperty] private Guid? _warehouseId;
    [ObservableProperty] private string _source = "Manual";
    [ObservableProperty] private Guid? _supplierId;
    [ObservableProperty] private DateTimeOffset? _expectedDate;
    [ObservableProperty] private string? _notes;

    public ObservableCollection<WarehouseDto> Warehouses { get; } = [];
    public ObservableCollection<SupplierDto> Suppliers { get; } = [];
    public ObservableCollection<ProductDto> Products { get; } = [];
    public ObservableCollection<ReceivingLineEditViewModel> Lines { get; } = [];

    public ReceivingOrderEditViewModel(IWarehouseService warehouseService)
    {
        _warehouseService = warehouseService;
        Lines.Add(new ReceivingLineEditViewModel());
    }

    public void SetData(IEnumerable<WarehouseDto> warehouses, IEnumerable<SupplierDto> suppliers, IEnumerable<ProductDto> products)
    {
        ReplaceItems(Warehouses, warehouses);
        ReplaceItems(Suppliers, suppliers);
        ReplaceItems(Products, products);
    }

    [RelayCommand]
    private void AddLine() => Lines.Add(new ReceivingLineEditViewModel());

    [RelayCommand]
    private void RemoveLine(ReceivingLineEditViewModel line)
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
        if (Lines.Any(l => l.ProductId is null || l.ExpectedQty <= 0))
        {
            ErrorMessage = "All lines must have a product and expected quantity > 0.";
            return;
        }

        ErrorMessage = null;
        try
        {
            var lines = Lines.Select(l => new ReceivingLineInput(l.ProductId!.Value, l.ExpectedQty)).ToList();
            var request = new CreateReceivingOrderRequest(WarehouseId.Value, Source, null, SupplierId, ExpectedDate, null, Notes, lines);
            var result = await _warehouseService.CreateReceivingOrderAsync(request);
            if (result is not null) SaveAndClose();
            else ErrorMessage = "Failed to create receiving order.";
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
    }
}

public partial class ReceivingLineEditViewModel : ObservableObject
{
    [ObservableProperty] private Guid? _productId;
    [ObservableProperty] private decimal _expectedQty = 1;
}
