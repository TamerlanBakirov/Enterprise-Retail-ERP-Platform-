using System.Windows;
using System.Windows.Controls;
using GeorgiaERP.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GeorgiaERP.Desktop.Views.Inventory;

public partial class InventoryView : UserControl
{
    private readonly InventoryViewModel _viewModel;

    public InventoryView()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<InventoryViewModel>();
        DataContext = _viewModel;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        IsVisibleChanged += OnVisibilityChanged;
        if (Visibility == Visibility.Visible && _viewModel.StockLevels.Count == 0)
            await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true && _viewModel.StockLevels.Count == 0)
            await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnCreateTransfer(object sender, RoutedEventArgs e)
    {
        var vm = App.Services.GetRequiredService<TransferOrderEditViewModel>();
        await vm.LoadDataAsync();
        var window = new TransferOrderEditWindow { DataContext = vm, Owner = Window.GetWindow(this) };
        window.ShowDialog();
        if (vm.Saved) await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnCreateStockCount(object sender, RoutedEventArgs e)
    {
        var vm = App.Services.GetRequiredService<StockCountEditViewModel>();
        await vm.LoadDataAsync();
        var window = new StockCountEditWindow { DataContext = vm, Owner = Window.GetWindow(this) };
        window.ShowDialog();
        if (vm.Saved) await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
