using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class CustomerEditViewModel : ObservableObject
{
    private readonly ICustomerService _customerService;

    [ObservableProperty] private string _firstName = string.Empty;
    [ObservableProperty] private string _lastName = string.Empty;
    [ObservableProperty] private string? _firstNameKa;
    [ObservableProperty] private string? _lastNameKa;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string? _email;
    [ObservableProperty] private string? _companyName;
    [ObservableProperty] private string? _taxId;
    [ObservableProperty] private string? _errorMessage;

    public bool Saved { get; private set; }

    public CustomerEditViewModel(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))
        {
            ErrorMessage = "First name and last name are required.";
            return;
        }
        if (string.IsNullOrWhiteSpace(Phone))
        {
            ErrorMessage = "Phone is required.";
            return;
        }

        ErrorMessage = null;
        try
        {
            var request = new CreateCustomerRequest(
                FirstName, LastName, FirstNameKa, LastNameKa,
                Phone, Email, CompanyName, TaxId);

            var result = await _customerService.CreateCustomerAsync(request);
            if (result is not null)
            {
                Saved = true;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (Window w in Application.Current.Windows)
                        if (w.DataContext == this) { w.Close(); break; }
                });
            }
            else
            {
                ErrorMessage = "Failed to save customer.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
