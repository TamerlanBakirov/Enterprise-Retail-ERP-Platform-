namespace GeorgiaERP.Desktop.Services;

public interface ISettingsService
{
    string ApiBaseUrl { get; set; }
    string? AccessToken { get; set; }
    string? RefreshToken { get; set; }
    string Language { get; set; }
    void Save();
    void Load();
}

public class SettingsService : ISettingsService
{
    private readonly string _settingsPath;

    public string ApiBaseUrl { get; set; } = "http://localhost:5000/api/v1/";
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string Language { get; set; } = "ka";

    public SettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "GeorgiaERP");
        Directory.CreateDirectory(dir);
        _settingsPath = Path.Combine(dir, "settings.json");
        Load();
    }

    public void Save()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new SettingsData
        {
            ApiBaseUrl = ApiBaseUrl,
            Language = Language
        });
        File.WriteAllText(_settingsPath, json);
    }

    public void Load()
    {
        if (!File.Exists(_settingsPath)) return;
        try
        {
            var json = File.ReadAllText(_settingsPath);
            var data = System.Text.Json.JsonSerializer.Deserialize<SettingsData>(json);
            if (data is null) return;
            ApiBaseUrl = data.ApiBaseUrl ?? ApiBaseUrl;
            Language = data.Language ?? Language;
        }
        catch { }
    }

    private class SettingsData
    {
        public string? ApiBaseUrl { get; set; }
        public string? Language { get; set; }
    }
}
