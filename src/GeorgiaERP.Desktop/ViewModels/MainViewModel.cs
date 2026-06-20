using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Services;
using GeorgiaERP.Desktop.Views.Login;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;
    private readonly IUpdateService _updateService;
    private readonly IOfflineQueueService _offlineQueue;

    [ObservableProperty] private string _currentView = "Dashboard";
    [ObservableProperty] private string _userDisplayName = string.Empty;
    [ObservableProperty] private string _userRole = string.Empty;
    [ObservableProperty] private bool _updateAvailable;
    [ObservableProperty] private string? _updateVersion;
    [ObservableProperty] private int _offlinePendingCount;

    private UpdateInfo? _pendingUpdate;

    public MainViewModel(
        IAuthService authService,
        INavigationService navigationService,
        IUpdateService updateService,
        IOfflineQueueService offlineQueue)
    {
        _authService = authService;
        _navigationService = navigationService;
        _updateService = updateService;
        _offlineQueue = offlineQueue;

        if (_authService.CurrentUser is { } user)
        {
            UserDisplayName = user.FullName;
            UserRole = user.Role;
        }

        _navigationService.ViewChanged += view => CurrentView = view;
        _offlineQueue.QueueChanged += () => OfflinePendingCount = _offlineQueue.PendingCount;
        OfflinePendingCount = _offlineQueue.PendingCount;

        _ = CheckForUpdateAsync();
        _ = FlushOfflineQueueAsync();
    }

    [RelayCommand]
    private void Navigate(string viewName)
    {
        _navigationService.NavigateTo(viewName);
    }

    [RelayCommand]
    private async Task Logout()
    {
        await _authService.LogoutAsync();
        var loginWindow = new LoginWindow();
        loginWindow.Show();
        Application.Current.MainWindow?.Close();
        Application.Current.MainWindow = loginWindow;
    }

    [RelayCommand]
    private void DismissUpdate()
    {
        UpdateAvailable = false;
    }

    [RelayCommand]
    private async Task DownloadUpdateAsync()
    {
        if (_pendingUpdate is null) return;
        try
        {
            var path = await _updateService.DownloadUpdateAsync(_pendingUpdate);
            _updateService.ApplyUpdate(path);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Update failed: {ex.Message}", "Georgia ERP", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private async Task RetryOfflineAsync()
    {
        await _offlineQueue.FlushAsync();
    }

    private async Task CheckForUpdateAsync()
    {
        try
        {
            _pendingUpdate = await _updateService.CheckForUpdateAsync();
            if (_pendingUpdate is not null)
            {
                UpdateVersion = _pendingUpdate.Version;
                UpdateAvailable = true;
            }
        }
        catch { }
    }

    private async Task FlushOfflineQueueAsync()
    {
        try { await _offlineQueue.FlushAsync(); }
        catch { }
    }
}
