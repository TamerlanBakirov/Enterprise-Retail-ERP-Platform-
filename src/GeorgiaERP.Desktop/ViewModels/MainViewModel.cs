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

    [ObservableProperty] private string _currentView = "Dashboard";
    [ObservableProperty] private string _userDisplayName = string.Empty;
    [ObservableProperty] private string _userRole = string.Empty;

    public MainViewModel(IAuthService authService, INavigationService navigationService)
    {
        _authService = authService;
        _navigationService = navigationService;

        if (_authService.CurrentUser is { } user)
        {
            UserDisplayName = user.FullName;
            UserRole = user.Role;
        }

        _navigationService.ViewChanged += view => CurrentView = view;
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
}
