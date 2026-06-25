using System.Windows;
using System.Windows.Controls;
using GeorgiaERP.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GeorgiaERP.Desktop.Views.Warehouse;

public partial class WarehouseView : UserControl
{
    private readonly WarehouseViewModel _viewModel;

    public WarehouseView()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<WarehouseViewModel>();
        DataContext = _viewModel;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        IsVisibleChanged += OnVisibilityChanged;
        if (Visibility == Visibility.Visible && _viewModel.ReceivingOrders.Count == 0)
            await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true && _viewModel.ReceivingOrders.Count == 0)
            await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnCreateReceiving(object sender, RoutedEventArgs e)
    {
        var vm = App.Services.GetRequiredService<ReceivingOrderEditViewModel>();
        var procService = App.Services.GetRequiredService<Services.IProcurementService>();
        var prodService = App.Services.GetRequiredService<Services.IProductService>();
        var orgService = App.Services.GetRequiredService<Services.IOrganizationService>();

        var warehouses = await orgService.GetWarehousesAsync(true);
        var suppliers = await procService.GetSuppliersAsync(isActive: true, pageSize: 200);
        var products = await prodService.GetProductsAsync(pageSize: 500);
        vm.SetData(warehouses, suppliers.Items, products.Items);

        var window = new ReceivingOrderEditWindow { DataContext = vm, Owner = Window.GetWindow(this) };
        window.ShowDialog();
        if (vm.Saved) await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnCreateShipping(object sender, RoutedEventArgs e)
    {
        var vm = App.Services.GetRequiredService<ShippingOrderEditViewModel>();
        var prodService = App.Services.GetRequiredService<Services.IProductService>();
        var orgService = App.Services.GetRequiredService<Services.IOrganizationService>();

        var warehouses = await orgService.GetWarehousesAsync(true);
        var products = await prodService.GetProductsAsync(pageSize: 500);
        vm.SetData(warehouses, products.Items);

        var window = new ShippingOrderEditWindow { DataContext = vm, Owner = Window.GetWindow(this) };
        window.ShowDialog();
        if (vm.Saved) await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnCreateLocation(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedWarehouse is null)
        {
            MessageBox.Show("Please select a warehouse first.", "Georgia ERP");
            return;
        }
        var vm = App.Services.GetRequiredService<LocationEditViewModel>();
        vm.SetWarehouse(_viewModel.SelectedWarehouse.Id);
        var window = new LocationEditWindow { DataContext = vm, Owner = Window.GetWindow(this) };
        window.ShowDialog();
        if (vm.Saved) await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
