using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeorgiaERP.Desktop.Services;

namespace GeorgiaERP.Desktop.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;

    [ObservableProperty] private string _serverUrl = string.Empty;
    [ObservableProperty] private string _language = "ka";
    [ObservableProperty] private string? _statusMessage;

    public SettingsViewModel(ISettingsService settings)
    {
        _settings = settings;
        ServerUrl = settings.ApiBaseUrl.Replace("/api/v1/", "");
        Language = settings.Language;
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
        StatusMessage = "Settings saved";
    }
}
