using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class SetPriceViewModel : DialogViewModel
{
    private readonly IPricingService _pricingService;

    [ObservableProperty] private Guid _priceListId;
    [ObservableProperty] private Guid? _productId;
    [ObservableProperty] private decimal _price;
    [ObservableProperty] private decimal _minQty = 1;

    public ObservableCollection<ProductDto> Products { get; } = [];

    public SetPriceViewModel(IPricingService pricingService)
    {
        _pricingService = pricingService;
    }

    public async Task LoadProductsAsync(IProductService productService)
    {
        try
        {
            var products = await productService.GetProductsAsync(pageSize: 500);
            ReplaceItems(Products, products.Items);
        }
        catch { }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (ProductId is null)
        {
            ErrorMessage = "Product is required.";
            return;
        }
        if (Price <= 0)
        {
            ErrorMessage = "Price must be greater than zero.";
            return;
        }

        ErrorMessage = null;
        try
        {
            var request = new SetPriceRequest(PriceListId, ProductId.Value, Price, MinQty, null);
            var result = await _pricingService.SetPriceAsync(request);
            if (result is not null) SaveAndClose();
            else ErrorMessage = "Failed to set price.";
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
    }
}
