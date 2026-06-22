using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly IReportService _reportService;
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;

    [ObservableProperty] private decimal _todayRevenue;
    [ObservableProperty] private int _todayTransactions;
    [ObservableProperty] private int _lowStockCount;
    [ObservableProperty] private int _totalProducts;
    [ObservableProperty] private decimal _totalStockValue;
    [ObservableProperty] private string _welcomeMessage = string.Empty;

    public DashboardViewModel(IReportService reportService, IAuthService authService, INavigationService navigationService)
    {
        _reportService = reportService;
        _authService = authService;
        _navigationService = navigationService;
        WelcomeMessage = $"Welcome, {authService.CurrentUser?.FullName ?? "User"}";
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
}
