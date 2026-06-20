using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IReportService _reportService;
    private readonly IInventoryService _inventoryService;
    private readonly IAuthService _authService;

    [ObservableProperty] private decimal _todayRevenue;
    [ObservableProperty] private int _todayTransactions;
    [ObservableProperty] private int _lowStockCount;
    [ObservableProperty] private int _totalProducts;
    [ObservableProperty] private decimal _totalStockValue;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string _welcomeMessage = string.Empty;

    public DashboardViewModel(IReportService reportService, IInventoryService inventoryService, IAuthService authService)
    {
        _reportService = reportService;
        _inventoryService = inventoryService;
        _authService = authService;
        WelcomeMessage = $"Welcome, {authService.CurrentUser?.FullName ?? "User"}";
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
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
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
