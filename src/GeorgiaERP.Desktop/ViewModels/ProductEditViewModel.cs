using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class ProductEditViewModel : ObservableObject
{
    private readonly IProductService _productService;

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
    [ObservableProperty] private string? _errorMessage;

    public ObservableCollection<CategoryDto> Categories { get; } = [];
    public bool Saved { get; private set; }

    public ProductEditViewModel(IProductService productService)
    {
        _productService = productService;
    }

    public async Task LoadCategoriesAsync()
    {
        try
        {
            var cats = await _productService.GetCategoriesAsync();
            foreach (var c in cats) Categories.Add(c);
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
            var request = new CreateProductRequest(
                Sku, Name, NameKa, null, CategoryId.Value,
                UnitOfMeasure, RetailPrice, WholesalePrice,
                VatRate, Barcode, false, false, false, null);

            var result = await _productService.CreateProductAsync(request);
            if (result is not null)
            {
                Saved = true;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (Window w in Application.Current.Windows)
                        if (w.DataContext == this) { w.Close(); break; }
                });
            }
            else
            {
                ErrorMessage = "Failed to save product.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
