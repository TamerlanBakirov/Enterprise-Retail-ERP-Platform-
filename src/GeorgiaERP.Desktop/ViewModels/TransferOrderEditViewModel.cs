using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class TransferOrderEditViewModel : DialogViewModel
{
    private readonly IInventoryService _inventoryService;
    private readonly IOrganizationService _organizationService;
    private readonly IProductService _productService;

    [ObservableProperty] private Guid? _sourceWarehouseId;
    [ObservableProperty] private Guid? _destinationWarehouseId;
    [ObservableProperty] private string? _notes;

    public ObservableCollection<WarehouseDto> Warehouses { get; } = [];
    public ObservableCollection<ProductDto> Products { get; } = [];
    public ObservableCollection<TransferLineViewModel> Lines { get; } = [];

    public TransferOrderEditViewModel(IInventoryService inventoryService, IOrganizationService organizationService, IProductService productService)
    {
        _inventoryService = inventoryService;
        _organizationService = organizationService;
        _productService = productService;
        Lines.Add(new TransferLineViewModel());
    }

    public async Task LoadDataAsync()
    {
        try
        {
            var warehouses = await _organizationService.GetWarehousesAsync(true);
            ReplaceItems(Warehouses, warehouses);
            var products = await _productService.GetProductsAsync(pageSize: 500);
            ReplaceItems(Products, products.Items);
        }
        catch { }
    }

    [RelayCommand]
    private void AddLine() => Lines.Add(new TransferLineViewModel());

    [RelayCommand]
    private void RemoveLine(TransferLineViewModel line)
    {
        if (Lines.Count > 1) Lines.Remove(line);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SourceWarehouseId is null || DestinationWarehouseId is null)
        {
            ErrorMessage = "Source and Destination warehouses are required.";
            return;
        }
        if (SourceWarehouseId == DestinationWarehouseId)
        {
            ErrorMessage = "Source and Destination must be different.";
            return;
        }
        if (Lines.Any(l => l.ProductId is null || l.Quantity <= 0))
        {
            ErrorMessage = "All lines must have a product and quantity > 0.";
            return;
        }

        ErrorMessage = null;
        try
        {
            var lines = Lines.Select(l => new TransferLineInput(l.ProductId!.Value, l.Quantity)).ToList();
            var request = new CreateTransferOrderRequest(SourceWarehouseId.Value, DestinationWarehouseId.Value, lines, Notes);
            var result = await _inventoryService.CreateTransferAsync(request);
            if (result is not null)
                SaveAndClose();
            else
                ErrorMessage = "Failed to create transfer order.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}

public partial class TransferLineViewModel : ObservableObject
{
    [ObservableProperty] private Guid? _productId;
    [ObservableProperty] private decimal _quantity = 1;
}
