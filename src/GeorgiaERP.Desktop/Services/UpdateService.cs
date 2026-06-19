using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;

namespace GeorgiaERP.Desktop.Services;

public interface IUpdateService
{
    Task<UpdateInfo?> CheckForUpdateAsync();
    Task<string> DownloadUpdateAsync(UpdateInfo update, IProgress<int>? progress = null, CancellationToken ct = default);
    void ApplyUpdate(string installerPath);
}

public record UpdateInfo(string Version, string DownloadUrl, string? ReleaseNotes, long FileSize);

public class UpdateService : IUpdateService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISettingsService _settings;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public UpdateService(IHttpClientFactory httpClientFactory, ISettingsService settings)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings;
    }

    public async Task<UpdateInfo?> CheckForUpdateAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("api");
            var response = await client.GetAsync("updates/latest");
            if (!response.IsSuccessStatusCode) return null;

            var update = await response.Content.ReadFromJsonAsync<UpdateInfo>(JsonOptions);
            if (update is null) return null;

            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            var latestVersion = Version.Parse(update.Version);

            return latestVersion > currentVersion ? update : null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<string> DownloadUpdateAsync(UpdateInfo update, IProgress<int>? progress = null, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("api");
        using var response = await client.GetAsync(update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var tempDir = Path.Combine(Path.GetTempPath(), "GeorgiaERP_Update");
        Directory.CreateDirectory(tempDir);
        var filePath = Path.Combine(tempDir, "GeorgiaERP_Setup.exe");

        await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);

        var totalBytes = response.Content.Headers.ContentLength ?? -1;
        var bytesRead = 0L;
        var buffer = new byte[81920];
        int read;

        while ((read = await contentStream.ReadAsync(buffer, ct)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read), ct);
            bytesRead += read;
            if (totalBytes > 0)
                progress?.Report((int)(bytesRead * 100 / totalBytes));
        }

        return filePath;
    }

    public void ApplyUpdate(string installerPath)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = installerPath,
            Arguments = "/SILENT",
            UseShellExecute = true
        });

        Environment.Exit(0);
    }
}
