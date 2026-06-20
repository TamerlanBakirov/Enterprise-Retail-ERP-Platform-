using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly ILicenseService _licenseService;
    private readonly IUpdateService _updateService;
    private readonly ILocalizationService _localization;

    [ObservableProperty] private string _serverUrl = string.Empty;
    [ObservableProperty] private string _language = "ka";
    [ObservableProperty] private string? _statusMessage;

    [ObservableProperty] private string _licenseStatus = "-";
    [ObservableProperty] private string _licenseCompany = "-";
    [ObservableProperty] private DateTimeOffset? _licenseExpires;
    [ObservableProperty] private int _licenseMaxUsers;
    [ObservableProperty] private int _licenseMaxStores;

    [ObservableProperty] private string _currentVersion = string.Empty;
    [ObservableProperty] private string? _updateStatus;

    public List<LanguageOption> Languages { get; } =
    [
        new("ka", "ქართული (Georgian)"),
        new("en", "English")
    ];

    public SettingsViewModel(
        ISettingsService settings,
        ILicenseService licenseService,
        IUpdateService updateService,
        ILocalizationService localization)
    {
        _settings = settings;
        _licenseService = licenseService;
        _updateService = updateService;
        _localization = localization;
        ServerUrl = settings.ApiBaseUrl.Replace("/api/v1/", "");
        Language = settings.Language;
        CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
    }

    [RelayCommand]
    private void Save()
    {
        var url = ServerUrl.TrimEnd('/');
        if (!url.EndsWith("/api/v1")) url += "/api/v1/";
        else url += "/";

        _settings.ApiBaseUrl = url;
        _settings.Language = Language;
        _settings.Save();
        _localization.SetLanguage(Language);
        StatusMessage = _localization.Get("settings.saved");
    }

    [RelayCommand]
    private async Task LoadLicenseAsync()
    {
        try
        {
            var info = await _licenseService.GetStatusAsync();
            if (info is not null)
            {
                LicenseStatus = info.IsValid ? _localization.Get("settings.license_valid") : (info.Error ?? "Invalid");
                LicenseCompany = info.CompanyName ?? "-";
                LicenseExpires = info.ExpiresAt;
                LicenseMaxUsers = info.MaxUsers;
                LicenseMaxStores = info.MaxStores;
            }
        }
        catch { }
    }

    [RelayCommand]
    private async Task CheckUpdatesAsync()
    {
        UpdateStatus = null;
        try
        {
            var update = await _updateService.CheckForUpdateAsync();
            UpdateStatus = update is not null
                ? $"{_localization.Get("settings.update_available")}: v{update.Version}"
                : _localization.Get("settings.no_updates");
        }
        catch (Exception ex)
        {
            UpdateStatus = ex.Message;
        }
    }
}

public record LanguageOption(string Code, string Display);
