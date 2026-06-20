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
        if (_viewModel.StockLevels.Count == 0)
            await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
