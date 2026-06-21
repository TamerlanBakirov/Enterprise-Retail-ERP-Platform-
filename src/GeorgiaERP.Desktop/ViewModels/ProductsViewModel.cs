using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class ProductsViewModel : PagedViewModel
{
    private readonly IProductService _productService;

    [ObservableProperty] private ProductDto? _selectedProduct;

    public ObservableCollection<ProductDto> Products { get; } = [];
    public ObservableCollection<CategoryDto> Categories { get; } = [];

    public ProductsViewModel(IProductService productService)
    {
        _productService = productService;
    }

    protected override async Task LoadCoreAsync()
    {
        var result = await _productService.GetProductsAsync(
            SearchFilter, page: CurrentPage);

        ReplaceItems(Products, result.Items);
        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages;

        if (Categories.Count == 0)
        {
            var cats = await _productService.GetCategoriesAsync();
            ReplaceItems(Categories, cats);
        }
    }
}
