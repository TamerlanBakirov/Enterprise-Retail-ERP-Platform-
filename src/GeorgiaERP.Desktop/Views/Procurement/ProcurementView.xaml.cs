using System.Windows;
using System.Windows.Controls;
using GeorgiaERP.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GeorgiaERP.Desktop.Views.Procurement;

public partial class ProcurementView : UserControl
{
    private readonly ProcurementViewModel _viewModel;

    public ProcurementView()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<ProcurementViewModel>();
        DataContext = _viewModel;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        IsVisibleChanged += OnVisibilityChanged;
        if (Visibility == Visibility.Visible && _viewModel.Suppliers.Count == 0 && _viewModel.PurchaseOrders.Count == 0)
            await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true && _viewModel.Suppliers.Count == 0 && _viewModel.PurchaseOrders.Count == 0)
            await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnAddSupplier(object sender, RoutedEventArgs e)
    {
        var vm = App.Services.GetRequiredService<SupplierEditViewModel>();
        var window = new SupplierEditWindow { DataContext = vm, Owner = Window.GetWindow(this) };
        window.ShowDialog();
        if (vm.Saved) await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnAddPurchaseOrder(object sender, RoutedEventArgs e)
    {
        var vm = App.Services.GetRequiredService<PurchaseOrderEditViewModel>();
        await vm.LoadDataAsync();
        var window = new PurchaseOrderEditWindow { DataContext = vm, Owner = Window.GetWindow(this) };
        window.ShowDialog();
        if (vm.Saved) await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
