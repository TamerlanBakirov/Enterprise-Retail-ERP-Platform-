using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class ProductEditViewModel : DialogViewModel
{
    private readonly IProductService _productService;
    private Guid? _editingProductId;

    [ObservableProperty] private string _windowTitle = "Add Product";
    [ObservableProperty] private string _sku = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _nameKa;
    [ObservableProperty] private Guid? _categoryId;
    [ObservableProperty] private decimal _retailPrice;
    [ObservableProperty] private decimal? _wholesalePrice;
    [ObservableProperty] private decimal _vatRate = 0.18m;
    [ObservableProperty] private string _unitOfMeasure = "pcs";
    [ObservableProperty] private string? _barcode;

    public ObservableCollection<CategoryDto> Categories { get; } = [];

    public ProductEditViewModel(IProductService productService)
    {
        _productService = productService;
    }

    public void LoadProduct(ProductDto product)
    {
        _editingProductId = product.Id;
        WindowTitle = "Edit Product";
        Sku = product.Sku;
        Name = product.Name;
        NameKa = product.NameKa;
        CategoryId = product.CategoryId;
        RetailPrice = product.RetailPrice;
        WholesalePrice = product.WholesalePrice;
        VatRate = product.VatRate;
        UnitOfMeasure = product.UnitOfMeasure;
        Barcode = product.Barcode;
    }

    public async Task LoadCategoriesAsync()
    {
        try
        {
            var cats = await _productService.GetCategoriesAsync();
            ReplaceItems(Categories, cats);
        }
        catch { }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Sku) || string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "SKU and Name are required.";
            return;
        }
        if (CategoryId is null)
        {
            ErrorMessage = "Category is required.";
            return;
        }

        ErrorMessage = null;
        try
        {
            if (_editingProductId.HasValue)
            {
                var update = new UpdateProductRequest(Name, NameKa, null, CategoryId, UnitOfMeasure, RetailPrice, WholesalePrice, VatRate, null);
                var result = await _productService.UpdateProductAsync(_editingProductId.Value, update);
                if (result.IsSuccess) SaveAndClose();
                else ErrorMessage = result.Error ?? "Failed to update product.";
            }
            else
            {
                var request = new CreateProductRequest(
                    Sku, Name, NameKa, null, CategoryId.Value,
                    UnitOfMeasure, RetailPrice, WholesalePrice,
                    VatRate, Barcode, false, false, false, null);

                var result = await _productService.CreateProductAsync(request);
                if (result is not null) SaveAndClose();
                else ErrorMessage = "Failed to save product.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
