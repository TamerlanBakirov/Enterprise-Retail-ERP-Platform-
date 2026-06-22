using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using GeorgiaERP.Desktop.Models;

namespace GeorgiaERP.Desktop.Services;

public interface IAuthService
{
    UserInfo? CurrentUser { get; }
    bool IsLoggedIn { get; }
    Task<(bool Success, string? Error)> LoginAsync(string username, string password, string? twoFactorCode = null);
    Task LogoutAsync();
    Task<TwoFactorSetupResponse?> BeginTwoFactorSetupAsync();
    Task<(bool Success, string? Error)> ConfirmTwoFactorSetupAsync(string code);
    Task<(bool Success, string? Error)> DisableTwoFactorAsync(string code);
    event Action? AuthStateChanged;
}

public class AuthService : IAuthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISettingsService _settings;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public UserInfo? CurrentUser { get; internal set; }
    public bool IsLoggedIn => CurrentUser is not null;
    public event Action? AuthStateChanged;

    public AuthService(IHttpClientFactory httpClientFactory, ISettingsService settings)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings;
    }

    public async Task<(bool Success, string? Error)> LoginAsync(string username, string password, string? twoFactorCode = null)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("api");
            var response = await client.PostAsJsonAsync("auth/login", new LoginRequest(username, password, twoFactorCode));

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return (false, error);
            }

            var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
            if (auth is null) return (false, "Invalid response");

            _settings.AccessToken = auth.AccessToken;
            _settings.RefreshToken = auth.RefreshToken;
            CurrentUser = auth.User;
            AuthStateChanged?.Invoke();
            return (true, null);
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Connection error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("api-auth");
            if (!string.IsNullOrWhiteSpace(_settings.RefreshToken))
                await client.PostAsJsonAsync("auth/logout", new RefreshTokenRequest(_settings.RefreshToken));
        }
        catch { }

        _settings.AccessToken = null;
        _settings.RefreshToken = null;
        CurrentUser = null;
        AuthStateChanged?.Invoke();
    }

    public async Task<TwoFactorSetupResponse?> BeginTwoFactorSetupAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("api-auth");
            var response = await client.PostAsync("auth/2fa/setup", null);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<TwoFactorSetupResponse>(JsonOptions);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<(bool Success, string? Error)> ConfirmTwoFactorSetupAsync(string code)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("api-auth");
            var response = await client.PostAsJsonAsync("auth/2fa/confirm", new TwoFactorConfirmRequest(code));
            if (!response.IsSuccessStatusCode)
                return (false, await response.Content.ReadAsStringAsync());
            return (true, null);
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Connection error: {ex.Message}");
        }
    }

    public async Task<(bool Success, string? Error)> DisableTwoFactorAsync(string code)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("api-auth");
            var response = await client.PostAsJsonAsync("auth/2fa/disable", new TwoFactorDisableRequest(code));
            if (!response.IsSuccessStatusCode)
                return (false, await response.Content.ReadAsStringAsync());
            return (true, null);
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Connection error: {ex.Message}");
        }
    }
}
