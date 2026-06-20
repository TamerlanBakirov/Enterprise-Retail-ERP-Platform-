using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Models;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class UserCreateViewModel : ObservableObject
{
    private readonly IUserService _userService;

    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _firstName = string.Empty;
    [ObservableProperty] private string _lastName = string.Empty;
    [ObservableProperty] private string? _firstNameKa;
    [ObservableProperty] private string? _lastNameKa;
    [ObservableProperty] private string? _phone;
    [ObservableProperty] private string _defaultLanguage = "ka";
    [ObservableProperty] private string? _errorMessage;

    public bool Saved { get; private set; }

    public UserCreateViewModel(IUserService userService)
    {
        _userService = userService;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Email) ||
            string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(FirstName) ||
            string.IsNullOrWhiteSpace(LastName))
        {
            ErrorMessage = "Username, email, password, first name and last name are required.";
            return;
        }

        ErrorMessage = null;
        try
        {
            var request = new CreateUserRequest(
                Username, Email, Password, FirstName, LastName,
                FirstNameKa, LastNameKa, Phone, null, DefaultLanguage, []);

            var result = await _userService.CreateUserAsync(request);
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
                ErrorMessage = "Failed to create user.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
