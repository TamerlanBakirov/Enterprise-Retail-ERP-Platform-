using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class PurchaseOrderEditViewModel : DialogViewModel
{
    private readonly IProcurementService _procurementService;
    private readonly IProductService _productService;
    private readonly IOrganizationService _organizationService;

    [ObservableProperty] private Guid? _supplierId;
    [ObservableProperty] private Guid? _warehouseId;
    [ObservableProperty] private string? _notes;

    public ObservableCollection<SupplierDto> Suppliers { get; } = [];
    public ObservableCollection<WarehouseDto> Warehouses { get; } = [];
    public ObservableCollection<ProductDto> Products { get; } = [];
    public ObservableCollection<PurchaseOrderLineViewModel> Lines { get; } = [];

    public PurchaseOrderEditViewModel(IProcurementService procurementService, IProductService productService, IOrganizationService organizationService)
    {
        _procurementService = procurementService;
        _productService = productService;
        _organizationService = organizationService;
        Lines.Add(new PurchaseOrderLineViewModel());
    }

    public async Task LoadDataAsync()
    {
        try
        {
            var suppliers = await _procurementService.GetSuppliersAsync(isActive: true, pageSize: 200);
            ReplaceItems(Suppliers, suppliers.Items);

            var warehouses = await _organizationService.GetWarehousesAsync(true);
            ReplaceItems(Warehouses, warehouses);

            var products = await _productService.GetProductsAsync(pageSize: 500);
            ReplaceItems(Products, products.Items);
        }
        catch { }
    }

    [RelayCommand]
    private void AddLine() => Lines.Add(new PurchaseOrderLineViewModel());

    [RelayCommand]
    private void RemoveLine(PurchaseOrderLineViewModel line)
    {
        if (Lines.Count > 1) Lines.Remove(line);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SupplierId is null || WarehouseId is null)
        {
            ErrorMessage = "Supplier and Warehouse are required.";
            return;
        }
        if (Lines.Any(l => l.ProductId is null || l.Quantity <= 0 || l.UnitPrice <= 0))
        {
            ErrorMessage = "All lines must have a product, quantity > 0, and unit price > 0.";
            return;
        }

        ErrorMessage = null;
        try
        {
            var lines = Lines.Select(l => new PurchaseOrderLineRequest(l.ProductId!.Value, l.Quantity, l.UnitPrice)).ToList();
            var request = new CreatePurchaseOrderRequest(SupplierId.Value, WarehouseId.Value, lines, Notes);
            var result = await _procurementService.CreatePurchaseOrderAsync(request);
            if (result is not null)
                SaveAndClose();
            else
                ErrorMessage = "Failed to save purchase order.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}

public partial class PurchaseOrderLineViewModel : ObservableObject
{
    [ObservableProperty] private Guid? _productId;
    [ObservableProperty] private decimal _quantity = 1;
    [ObservableProperty] private decimal _unitPrice;
}
