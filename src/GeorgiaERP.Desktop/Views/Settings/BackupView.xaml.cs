using System.Windows;
using System.Windows.Controls;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GeorgiaERP.Desktop.Views.Settings;

public partial class BackupView : UserControl
{
    private readonly BackupViewModel _viewModel;

    public BackupView()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<BackupViewModel>();
        DataContext = _viewModel;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        IsVisibleChanged += OnVisibilityChanged;
        if (Visibility == Visibility.Visible && _viewModel.Backups.Count == 0)
            await _viewModel.LoadBackupsCommand.ExecuteAsync(null);
    }

    private async void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true && _viewModel.Backups.Count == 0)
            await _viewModel.LoadBackupsCommand.ExecuteAsync(null);
    }

    private async void OnRestore(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not BackupRecordDto backup)
            return;

        var confirm = MessageBox.Show(
            $"Restore the database from backup '{backup.FileName}'?\n\n" +
            "This OVERWRITES all current data and cannot be undone.",
            "Confirm Restore", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (confirm == MessageBoxResult.Yes)
            await _viewModel.RestoreBackupCommand.ExecuteAsync(backup);
    }

    private async void OnDelete(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not BackupRecordDto backup)
            return;

        var confirm = MessageBox.Show(
            $"Delete backup '{backup.FileName}'?",
            "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (confirm == MessageBoxResult.Yes)
            await _viewModel.DeleteBackupCommand.ExecuteAsync(backup);
    }
}
