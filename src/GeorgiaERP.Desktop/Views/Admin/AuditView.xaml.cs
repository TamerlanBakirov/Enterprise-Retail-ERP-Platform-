using System.Windows;
using System.Windows.Controls;
using GeorgiaERP.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GeorgiaERP.Desktop.Views.Admin;

public partial class AuditView : UserControl
{
    private readonly AuditViewModel _viewModel;

    public AuditView()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<AuditViewModel>();
        DataContext = _viewModel;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        IsVisibleChanged += OnVisibilityChanged;
        if (Visibility == Visibility.Visible && _viewModel.AuditLogs.Count == 0)
            await _viewModel.LoadAuditLogsCommand.ExecuteAsync(null);
    }

    private async void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true && _viewModel.AuditLogs.Count == 0)
            await _viewModel.LoadAuditLogsCommand.ExecuteAsync(null);
    }
}
