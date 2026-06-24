using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class CustomersViewModel : PagedViewModel
{
    private readonly ICustomerService _customerService;

    [ObservableProperty] private CustomerDto? _selectedCustomer;
    [ObservableProperty] private string? _loyaltyAdminStatus;

    public ObservableCollection<CustomerDto> Customers { get; } = [];
    public ObservableCollection<LoyaltyTransactionDto> LoyaltyHistory { get; } = [];

    public CustomersViewModel(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    protected override async Task LoadCoreAsync()
    {
        var result = await _customerService.GetCustomersAsync(
            SearchFilter, page: CurrentPage);

        ReplaceItems(Customers, result.Items);
        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages;
    }

    // Loads the selected customer's loyalty ledger into the side panel.
    partial void OnSelectedCustomerChanged(CustomerDto? value)
    {
        LoyaltyHistory.Clear();
        if (value is null) return;
        _ = LoadLoyaltyHistoryAsync(value.Id);
    }

    private async Task LoadLoyaltyHistoryAsync(Guid customerId)
    {
        var history = await _customerService.GetLoyaltyHistoryAsync(customerId);
        // Selection may have changed while awaiting; only apply if still current.
        if (SelectedCustomer?.Id != customerId) return;
        ReplaceItems(LoyaltyHistory, history.Items);
    }

    [RelayCommand]
    private async Task RecalculateTiersAsync()
    {
        await ExecuteAsync(async () =>
        {
            var result = await _customerService.RecalculateLoyaltyTiersAsync();
            LoyaltyAdminStatus = result.IsSuccess ? "Loyalty tiers recalculated." : result.Error;
            if (result.IsSuccess) await LoadAsync();
        });
    }

    [RelayCommand]
    private async Task ExpirePointsAsync()
    {
        await ExecuteAsync(async () =>
        {
            var result = await _customerService.ExpireLoyaltyPointsAsync(12);
            LoyaltyAdminStatus = result.IsSuccess ? "Inactive loyalty points expired." : result.Error;
            if (result.IsSuccess) await LoadAsync();
        });
    }
}
