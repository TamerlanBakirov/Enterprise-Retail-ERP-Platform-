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
        if (_viewModel.Suppliers.Count == 0 && _viewModel.PurchaseOrders.Count == 0)
            await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
