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
        if (_viewModel.ReceivingOrders.Count == 0)
            await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
