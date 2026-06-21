using System.Windows;
using System.Windows.Controls;
using GeorgiaERP.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GeorgiaERP.Desktop.Views.Compliance;

public partial class ComplianceView : UserControl
{
    private readonly ComplianceViewModel _viewModel;

    public ComplianceView()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<ComplianceViewModel>();
        DataContext = _viewModel;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel.Waybills.Count == 0)
            await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnCreateWaybill(object sender, RoutedEventArgs e)
    {
        var vm = App.Services.GetRequiredService<WaybillEditViewModel>();
        var window = new WaybillEditWindow { DataContext = vm, Owner = Window.GetWindow(this) };
        window.ShowDialog();
        if (vm.Saved) await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
