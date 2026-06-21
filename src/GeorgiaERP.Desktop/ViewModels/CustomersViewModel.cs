using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class CustomersViewModel : PagedViewModel
{
    private readonly ICustomerService _customerService;

    [ObservableProperty] private CustomerDto? _selectedCustomer;

    public ObservableCollection<CustomerDto> Customers { get; } = [];

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
}
