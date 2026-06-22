using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly IReportService _reportService;
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;
    private readonly ISignalRNotificationService _signalR;

    [ObservableProperty] private decimal _todayRevenue;
    [ObservableProperty] private int _todayTransactions;
    [ObservableProperty] private int _lowStockCount;
    [ObservableProperty] private int _totalProducts;
    [ObservableProperty] private decimal _totalStockValue;
    [ObservableProperty] private string _welcomeMessage = string.Empty;
    [ObservableProperty] private bool _isRealTimeConnected;

    /// <summary>
    /// Event types that trigger an auto-refresh of dashboard data.
    /// </summary>
    private static readonly HashSet<string> RefreshTriggerEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "LowStockAlert",
        "StockAdjusted",
        "PosTransactionCompleted",
        "DailyClosingCompleted",
        "OrderPlaced"
    };

    public DashboardViewModel(
        IReportService reportService,
        IAuthService authService,
        INavigationService navigationService,
        ISignalRNotificationService signalR)
    {
        _reportService = reportService;
        _authService = authService;
        _navigationService = navigationService;
        _signalR = signalR;
        WelcomeMessage = $"Welcome, {authService.CurrentUser?.FullName ?? "User"}";

        _signalR.NotificationReceived += OnNotificationReceived;
        _signalR.ConnectionStateChanged += connected =>
            Application.Current.Dispatcher.Invoke(() => IsRealTimeConnected = connected);
        IsRealTimeConnected = _signalR.IsConnected;
    }

    [RelayCommand]
    private Task LoadDataAsync() => ExecuteAsync(async () =>
    {
        var today = DateTimeOffset.UtcNow.Date;
        var salesTask = _reportService.GetSalesReportAsync(null, today, today.AddDays(1));
        var stockTask = _reportService.GetStockReportAsync();

        await Task.WhenAll(salesTask, stockTask);

        var sales = await salesTask;
        if (sales is not null)
        {
            TodayRevenue = sales.TotalRevenue;
            TodayTransactions = sales.TransactionCount;
        }

        var stock = await stockTask;
        if (stock is not null)
        {
            LowStockCount = stock.LowStockCount;
            TotalProducts = stock.TotalProducts;
            TotalStockValue = stock.TotalStockValue;
        }
    });

    [RelayCommand]
    private void GoToPos() => _navigationService.NavigateTo("POS");

    [RelayCommand]
    private void GoToProducts() => _navigationService.NavigateTo("Products");

    [RelayCommand]
    private void GoToInventory() => _navigationService.NavigateTo("Inventory");

    [RelayCommand]
    private void GoToReports() => _navigationService.NavigateTo("Reports");

    private void OnNotificationReceived(string eventType, JsonElement payload)
    {
        if (RefreshTriggerEvents.Contains(eventType))
        {
            // Auto-refresh dashboard data when relevant events occur
            Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await LoadDataCommand.ExecuteAsync(null);
            });
        }
    }
}
