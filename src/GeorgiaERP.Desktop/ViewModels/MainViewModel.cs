using System.Collections.ObjectModel;
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
    private readonly ISignalRNotificationService _signalR;
    private readonly IToastNotificationService _toastService;

    [ObservableProperty] private string _currentView = "Dashboard";
    [ObservableProperty] private string _userDisplayName = string.Empty;
    [ObservableProperty] private string _userRole = string.Empty;
    [ObservableProperty] private bool _updateAvailable;
    [ObservableProperty] private string? _updateVersion;
    [ObservableProperty] private int _offlinePendingCount;
    [ObservableProperty] private bool _isSignalRConnected;

    private UpdateInfo? _pendingUpdate;

    /// <summary>
    /// Active toast notifications for binding in the UI.
    /// </summary>
    public ObservableCollection<ToastMessage> ActiveToasts => _toastService.ActiveToasts;

    public MainViewModel(
        IAuthService authService,
        INavigationService navigationService,
        IUpdateService updateService,
        IOfflineQueueService offlineQueue,
        ISignalRNotificationService signalR,
        IToastNotificationService toastService)
    {
        _authService = authService;
        _navigationService = navigationService;
        _updateService = updateService;
        _offlineQueue = offlineQueue;
        _signalR = signalR;
        _toastService = toastService;

        if (_authService.CurrentUser is { } user)
        {
            UserDisplayName = user.FullName;
            UserRole = user.Role;
        }

        _navigationService.ViewChanged += view => CurrentView = view;
        _offlineQueue.QueueChanged += () => OfflinePendingCount = _offlineQueue.PendingCount;
        OfflinePendingCount = _offlineQueue.PendingCount;

        _signalR.ConnectionStateChanged += connected =>
        {
            Application.Current.Dispatcher.Invoke(() => IsSignalRConnected = connected);
        };

        _ = CheckForUpdateAsync();
        _ = FlushOfflineQueueAsync();
        _ = ConnectSignalRAsync();
    }

    [RelayCommand]
    private void Navigate(string viewName)
    {
        _navigationService.NavigateTo(viewName);
    }

    [RelayCommand]
    private async Task Logout()
    {
        _toastService.StopListening();
        await _signalR.DisconnectAsync();
        await _authService.LogoutAsync();
        var loginWindow = new LoginWindow();
        loginWindow.Show();
        Application.Current.MainWindow?.Close();
        Application.Current.MainWindow = loginWindow;
    }

    [RelayCommand]
    private void DismissToast(ToastMessage toast)
    {
        _toastService.DismissToast(toast);
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

    private async Task ConnectSignalRAsync()
    {
        try
        {
            await _signalR.ConnectAsync();
            _toastService.StartListening();
        }
        catch
        {
            // SignalR connection failures are non-fatal; the service will auto-reconnect.
        }
    }
}
