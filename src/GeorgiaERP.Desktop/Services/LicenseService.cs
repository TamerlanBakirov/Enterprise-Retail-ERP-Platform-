using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using GeorgiaERP.Desktop.Models;

namespace GeorgiaERP.Desktop.Services;

public interface ILicenseService
{
    Task<LicenseInfo?> GetStatusAsync();
    Task<LicenseActivationResult?> ActivateAsync(string licenseKey, string companyName, string? contactEmail);
    Task<bool> DeactivateAsync();
    Task<LicenseActivationResult?> RenewAsync(string licenseKey);
}

public record LicenseActivationResult(
    bool Activated,
    string? CompanyName,
    DateTimeOffset? ExpiresAt,
    int MaxUsers,
    int MaxStores);

public class LicenseService : ILicenseService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISettingsService _settings;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public LicenseService(IHttpClientFactory httpClientFactory, ISettingsService settings)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings;
    }

    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient("api");
        return client;
    }

    public async Task<LicenseInfo?> GetStatusAsync()
    {
        try
        {
            var client = CreateClient();
            return await client.GetFromJsonAsync<LicenseInfo>("license/status", JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task<LicenseActivationResult?> ActivateAsync(string licenseKey, string companyName, string? contactEmail)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("license/activate",
            new { licenseKey, companyName, contactEmail }, JsonOptions);

        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<LicenseActivationResult>(JsonOptions);
    }

    public async Task<bool> DeactivateAsync()
    {
        var client = CreateClient();
        var response = await client.PostAsync("license/deactivate", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<LicenseActivationResult?> RenewAsync(string licenseKey)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("license/renew", new { licenseKey }, JsonOptions);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<LicenseActivationResult>(JsonOptions);
    }
}
