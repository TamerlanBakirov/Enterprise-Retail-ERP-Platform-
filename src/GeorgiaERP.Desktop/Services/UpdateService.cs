using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace GeorgiaERP.Desktop.Services;

public interface IUpdateService
{
    Task<UpdateInfo?> CheckForUpdateAsync();
    Task<string> DownloadUpdateAsync(UpdateInfo update, IProgress<int>? progress = null, CancellationToken ct = default);
    void ApplyUpdate(string installerPath);
}

public record UpdateInfo(string Version, string DownloadUrl, string? ReleaseNotes, long FileSize, string Sha256);

public class UpdateService : IUpdateService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISettingsService _settings;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private string? _verifiedInstallerPath;
    private byte[]? _verifiedInstallerHash;

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
            if (!Uri.TryCreate(update.DownloadUrl, UriKind.Absolute, out var downloadUri) ||
                downloadUri.Scheme != Uri.UriSchemeHttps ||
                string.IsNullOrWhiteSpace(update.Sha256) || update.Sha256.Length != 64)
                return null;

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
        if (!Uri.TryCreate(update.DownloadUrl, UriKind.Absolute, out var downloadUri) ||
            downloadUri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Update packages must be downloaded over HTTPS.");
        if (string.IsNullOrWhiteSpace(update.Sha256) || update.Sha256.Length != 64)
            throw new InvalidOperationException("Update manifest does not contain a valid SHA-256 digest.");
        using var response = await client.GetAsync(downloadUri, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var tempDir = Path.Combine(Path.GetTempPath(), "GeorgiaERP_Update");
        Directory.CreateDirectory(tempDir);
        var filePath = Path.Combine(tempDir, "GeorgiaERP_Setup.exe");

        await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);

        var totalBytes = response.Content.Headers.ContentLength ?? -1;
        var bytesRead = 0L;
        var buffer = new byte[81920];
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        int read;

        while ((read = await contentStream.ReadAsync(buffer, ct)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read), ct);
            hash.AppendData(buffer, 0, read);
            bytesRead += read;
            if (totalBytes > 0)
                progress?.Report((int)(bytesRead * 100 / totalBytes));
        }

        await fileStream.FlushAsync(ct);
        var actualHash = hash.GetHashAndReset();
        byte[] expectedHash;
        try { expectedHash = Convert.FromHexString(update.Sha256); }
        catch (FormatException) { throw new InvalidOperationException("Update manifest contains an invalid SHA-256 digest."); }
        if (!CryptographicOperations.FixedTimeEquals(actualHash, expectedHash))
        {
            fileStream.Close();
            File.Delete(filePath);
            throw new InvalidDataException("Downloaded update failed integrity verification.");
        }

        _verifiedInstallerPath = Path.GetFullPath(filePath);
        _verifiedInstallerHash = expectedHash;

        return filePath;
    }

    public void ApplyUpdate(string installerPath)
    {
        var fullPath = Path.GetFullPath(installerPath);
        if (_verifiedInstallerPath is null || _verifiedInstallerHash is null ||
            !string.Equals(fullPath, _verifiedInstallerPath, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
            throw new InvalidOperationException("The update package has not been verified.");

        using var installer = File.OpenRead(fullPath);
        var currentHash = SHA256.HashData(installer);
        if (!CryptographicOperations.FixedTimeEquals(currentHash, _verifiedInstallerHash))
            throw new InvalidDataException("The verified update package was modified before installation.");

        Process.Start(new ProcessStartInfo
        {
            FileName = fullPath,
            Arguments = "/SILENT",
            UseShellExecute = true
        });

        Environment.Exit(0);
    }
}
