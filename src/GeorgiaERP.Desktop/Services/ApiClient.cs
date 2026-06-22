using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using GeorgiaERP.Desktop.Models;

namespace GeorgiaERP.Desktop.Services;

public interface IApiClient
{
    Task<T?> GetAsync<T>(string endpoint, CancellationToken ct = default);
    Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest body, CancellationToken ct = default);
    Task<ApiResult> PostAsync<TRequest>(string endpoint, TRequest body, CancellationToken ct = default);
    Task<ApiResult> PostAsync(string endpoint, CancellationToken ct = default);
    Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest body, CancellationToken ct = default);
    Task<ApiResult> PutAsync<TRequest>(string endpoint, TRequest body, CancellationToken ct = default);
    Task<ApiResult> DeleteAsync(string endpoint, CancellationToken ct = default);
}

public class ApiClient : IApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ApiClient(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    private HttpClient CreateClient() => _httpClientFactory.CreateClient("api-auth");

    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken ct = default)
    {
        try
        {
            var client = CreateClient();
            var response = await client.GetAsync(endpoint, ct);
            if (!response.IsSuccessStatusCode) return default;
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
        }
        catch (HttpRequestException)
        {
            return default;
        }
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest body, CancellationToken ct = default)
    {
        try
        {
            var client = CreateClient();
            var response = await client.PostAsJsonAsync(endpoint, body, JsonOptions, ct);
            if (!response.IsSuccessStatusCode) return default;
            return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, ct);
        }
        catch (HttpRequestException)
        {
            return default;
        }
    }

    public async Task<ApiResult> PostAsync<TRequest>(string endpoint, TRequest body, CancellationToken ct = default)
    {
        try
        {
            var client = CreateClient();
            var response = await client.PostAsJsonAsync(endpoint, body, JsonOptions, ct);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                return new ApiResult { IsSuccess = false, Error = error };
            }
            return new ApiResult { IsSuccess = true };
        }
        catch (HttpRequestException ex)
        {
            return new ApiResult { IsSuccess = false, Error = ex.Message };
        }
    }

    public async Task<ApiResult> PostAsync(string endpoint, CancellationToken ct = default)
    {
        try
        {
            var client = CreateClient();
            var response = await client.PostAsync(endpoint, null, ct);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                return new ApiResult { IsSuccess = false, Error = error };
            }
            return new ApiResult { IsSuccess = true };
        }
        catch (HttpRequestException ex)
        {
            return new ApiResult { IsSuccess = false, Error = ex.Message };
        }
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest body, CancellationToken ct = default)
    {
        try
        {
            var client = CreateClient();
            var response = await client.PutAsJsonAsync(endpoint, body, JsonOptions, ct);
            if (!response.IsSuccessStatusCode) return default;
            return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, ct);
        }
        catch (HttpRequestException)
        {
            return default;
        }
    }

    public async Task<ApiResult> PutAsync<TRequest>(string endpoint, TRequest body, CancellationToken ct = default)
    {
        try
        {
            var client = CreateClient();
            var response = await client.PutAsJsonAsync(endpoint, body, JsonOptions, ct);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                return new ApiResult { IsSuccess = false, Error = error };
            }
            return new ApiResult { IsSuccess = true };
        }
        catch (HttpRequestException ex)
        {
            return new ApiResult { IsSuccess = false, Error = ex.Message };
        }
    }

    public async Task<ApiResult> DeleteAsync(string endpoint, CancellationToken ct = default)
    {
        try
        {
            var client = CreateClient();
            var response = await client.DeleteAsync(endpoint, ct);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                return new ApiResult { IsSuccess = false, Error = error };
            }
            return new ApiResult { IsSuccess = true };
        }
        catch (HttpRequestException ex)
        {
            return new ApiResult { IsSuccess = false, Error = ex.Message };
        }
    }
}
