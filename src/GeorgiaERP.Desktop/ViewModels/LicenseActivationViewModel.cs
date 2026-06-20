using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Services;
using GeorgiaERP.Desktop.Views.Login;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class LicenseActivationViewModel : ObservableObject
{
    private readonly ILicenseService _licenseService;
    private readonly ISettingsService _settings;

    [ObservableProperty] private string _licenseKey = string.Empty;
    [ObservableProperty] private string _companyName = string.Empty;
    [ObservableProperty] private string _contactEmail = string.Empty;
    [ObservableProperty] private string _serverUrl = string.Empty;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string? _successMessage;
    [ObservableProperty] private bool _isLoading;

    public LicenseActivationViewModel(ILicenseService licenseService, ISettingsService settings)
    {
        _licenseService = licenseService;
        _settings = settings;
        _serverUrl = settings.ApiBaseUrl.Replace("/api/v1/", "");
    }

    [RelayCommand]
    private async Task ActivateAsync()
    {
        if (string.IsNullOrWhiteSpace(LicenseKey))
        {
            ErrorMessage = "License key is required.";
            return;
        }
        if (string.IsNullOrWhiteSpace(CompanyName))
        {
            ErrorMessage = "Company name is required.";
            return;
        }

        ErrorMessage = null;
        SuccessMessage = null;
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

            var result = await _licenseService.ActivateAsync(
                LicenseKey, CompanyName,
                string.IsNullOrWhiteSpace(ContactEmail) ? null : ContactEmail);

            if (result is { Activated: true })
            {
                SuccessMessage = $"License activated! Expires: {result.ExpiresAt:d}";
                await Task.Delay(1500);

                var loginWindow = new LoginWindow();
                loginWindow.Show();
                Application.Current.MainWindow?.Close();
                Application.Current.MainWindow = loginWindow;
            }
            else
            {
                ErrorMessage = "Activation failed. Check your license key.";
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
