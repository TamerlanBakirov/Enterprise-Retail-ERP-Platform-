using System.Windows;
using System.Windows.Controls;
using GeorgiaERP.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GeorgiaERP.Desktop.Views.Settings;

public partial class SettingsView : UserControl
{
    private readonly SettingsViewModel _viewModel;

    public SettingsView()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<SettingsViewModel>();
        DataContext = _viewModel;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadLicenseCommand.ExecuteAsync(null);
    }
}
