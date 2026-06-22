using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using GeorgiaERP.Desktop.Services;
using GeorgiaERP.Desktop.Views.Shell;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly ISettingsService _settings;

    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _serverUrl = string.Empty;

    public LoginViewModel(IAuthService authService, ISettingsService settings)
    {
        _authService = authService;
        _settings = settings;
        _serverUrl = settings.ApiBaseUrl.Replace("/api/v1/", "");
    }

    public async Task LoginAsync(string password, string? twoFactorCode)
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(password))
        {
            ErrorMessage = "Username and password are required";
            return;
        }

        await ExecuteAsync(async () =>
        {
            if (!string.IsNullOrWhiteSpace(ServerUrl))
            {
                var url = ServerUrl.TrimEnd('/');
                if (!Uri.TryCreate(url, UriKind.Absolute, out var serverUri) ||
                    (serverUri.Scheme != Uri.UriSchemeHttps && !serverUri.IsLoopback))
                {
                    ErrorMessage = "Remote servers must use HTTPS";
                    return;
                }
                if (!url.EndsWith("/api/v1")) url += "/api/v1/";
                else url += "/";
                _settings.ApiBaseUrl = url;
                _settings.Save();
            }

            var (success, error) = await _authService.LoginAsync(Username, password, twoFactorCode);
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
        });
    }
}
