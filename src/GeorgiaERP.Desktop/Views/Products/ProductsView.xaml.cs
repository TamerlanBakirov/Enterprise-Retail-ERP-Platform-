using System.Windows;
using System.Windows.Controls;
using GeorgiaERP.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GeorgiaERP.Desktop.Views.Products;

public partial class ProductsView : UserControl
{
    private readonly ProductsViewModel _viewModel;

    public ProductsView()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<ProductsViewModel>();
        DataContext = _viewModel;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel.Products.Count == 0)
            await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnAddProduct(object sender, RoutedEventArgs e)
    {
        var vm = App.Services.GetRequiredService<ProductEditViewModel>();
        await vm.LoadCategoriesAsync();
        var window = new ProductEditWindow { DataContext = vm, Owner = Window.GetWindow(this) };
        window.ShowDialog();
        if (vm.Saved)
            await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnEditProduct(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedProduct is null)
        {
            MessageBox.Show("Please select a product first.", "Georgia ERP");
            return;
        }
        var vm = App.Services.GetRequiredService<ProductEditViewModel>();
        await vm.LoadCategoriesAsync();
        vm.LoadProduct(_viewModel.SelectedProduct);
        var window = new ProductEditWindow { DataContext = vm, Owner = Window.GetWindow(this) };
        window.ShowDialog();
        if (vm.Saved)
            await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
