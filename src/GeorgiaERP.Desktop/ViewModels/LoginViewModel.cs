using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Services;
using GeorgiaERP.Desktop.Views.Shell;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly ISettingsService _settings;

    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _serverUrl = string.Empty;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isLoading;

    public LoginViewModel(IAuthService authService, ISettingsService settings)
    {
        _authService = authService;
        _settings = settings;
        _serverUrl = settings.ApiBaseUrl.Replace("/api/v1/", "");
    }

    public async Task LoginAsync(string password)
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(password))
        {
            ErrorMessage = "Username and password are required";
            return;
        }

        ErrorMessage = null;
        IsLoading = true;

        try
        {
            if (!string.IsNullOrWhiteSpace(ServerUrl))
            {
                var url = ServerUrl.TrimEnd('/');
                if (!url.EndsWith("/api/v1")) url += "/api/v1/";
                else url += "/";
                _settings.ApiBaseUrl = url;
                _settings.Save();
            }

            var (success, error) = await _authService.LoginAsync(Username, password);
            if (success)
            {
                var mainWindow = new MainWindow();
                mainWindow.Show();
                Application.Current.MainWindow?.Close();
                Application.Current.MainWindow = mainWindow;
            }
            else
            {
                ErrorMessage = error ?? "Login failed";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Connection error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
