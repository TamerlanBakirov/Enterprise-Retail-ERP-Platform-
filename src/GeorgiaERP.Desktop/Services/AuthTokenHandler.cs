using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GeorgiaERP.Desktop.Models;

namespace GeorgiaERP.Desktop.Services;

public class AuthTokenHandler : DelegatingHandler
{
    private readonly ISettingsService _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public AuthTokenHandler(ISettingsService settings, IHttpClientFactory httpClientFactory)
    {
        _settings = settings;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_settings.AccessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.AccessToken);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(_settings.RefreshToken))
        {
            await _refreshLock.WaitAsync(cancellationToken);
            try
            {
                var refreshed = await TryRefreshTokenAsync(cancellationToken);
                if (refreshed)
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.AccessToken);
                    response = await base.SendAsync(request, cancellationToken);
                }
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        return response;
    }

    private async Task<bool> TryRefreshTokenAsync(CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("api");
            var response = await client.PostAsJsonAsync("auth/refresh",
                new RefreshTokenRequest(_settings.RefreshToken!), ct);

            if (!response.IsSuccessStatusCode) return false;

            var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(ct);
            if (auth is null) return false;

            _settings.AccessToken = auth.AccessToken;
            _settings.RefreshToken = auth.RefreshToken;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
