using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace GeorgiaERP.Desktop.Services;

/// <summary>
/// Connects the WPF Desktop app to the server's SignalR notification hub.
/// Manages the connection lifecycle, automatic reconnection, and event dispatching.
/// </summary>
public interface ISignalRNotificationService : IAsyncDisposable
{
    /// <summary>
    /// Connects to the SignalR hub using the current JWT access token.
    /// </summary>
    Task ConnectAsync(CancellationToken ct = default);

    /// <summary>
    /// Disconnects from the SignalR hub.
    /// </summary>
    Task DisconnectAsync(CancellationToken ct = default);

    /// <summary>
    /// True when the hub connection is active.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Fired when any notification arrives from the server.
    /// Parameters: eventType (string), payload (JsonElement).
    /// </summary>
    event Action<string, JsonElement>? NotificationReceived;

    /// <summary>
    /// Fired when the connection state changes.
    /// </summary>
    event Action<bool>? ConnectionStateChanged;

    /// <summary>
    /// Join a notification group (e.g., "warehouse-{id}", "store-{id}").
    /// </summary>
    Task JoinGroupAsync(string groupName, CancellationToken ct = default);

    /// <summary>
    /// Leave a notification group.
    /// </summary>
    Task LeaveGroupAsync(string groupName, CancellationToken ct = default);
}

public sealed class SignalRNotificationService : ISignalRNotificationService
{
    private readonly ISettingsService _settings;
    private HubConnection? _connection;
    private bool _disposed;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;
    public event Action<string, JsonElement>? NotificationReceived;
    public event Action<bool>? ConnectionStateChanged;

    public SignalRNotificationService(ISettingsService settings)
    {
        _settings = settings;
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        if (_disposed) return;
        if (_connection is not null && _connection.State == HubConnectionState.Connected)
            return;

        await DisposeConnectionAsync();

        var hubUrl = BuildHubUrl();

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(_settings.AccessToken);
            })
            .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
            .AddJsonProtocol()
            .Build();

        _connection.On<string, JsonElement>("ReceiveNotification", OnNotificationReceived);
        _connection.On<string>("Connected", OnConnectedConfirmed);

        _connection.Reconnecting += _ =>
        {
            ConnectionStateChanged?.Invoke(false);
            return Task.CompletedTask;
        };

        _connection.Reconnected += _ =>
        {
            ConnectionStateChanged?.Invoke(true);
            return Task.CompletedTask;
        };

        _connection.Closed += _ =>
        {
            ConnectionStateChanged?.Invoke(false);
            return Task.CompletedTask;
        };

        try
        {
            await _connection.StartAsync(ct);
            ConnectionStateChanged?.Invoke(true);
        }
        catch
        {
            ConnectionStateChanged?.Invoke(false);
            throw;
        }
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        if (_connection is not null)
        {
            try
            {
                await _connection.StopAsync(ct);
            }
            catch
            {
                // Best-effort disconnect
            }
            ConnectionStateChanged?.Invoke(false);
        }
    }

    public async Task JoinGroupAsync(string groupName, CancellationToken ct = default)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync("JoinGroup", groupName, ct);
        }
    }

    public async Task LeaveGroupAsync(string groupName, CancellationToken ct = default)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync("LeaveGroup", groupName, ct);
        }
    }

    private void OnNotificationReceived(string eventType, JsonElement payload)
    {
        NotificationReceived?.Invoke(eventType, payload);
    }

    private void OnConnectedConfirmed(string connectionId)
    {
        // Connection confirmed by server
    }

    private string BuildHubUrl()
    {
        var baseUrl = _settings.ApiBaseUrl.TrimEnd('/');
        // Remove /api/v1 suffix to get to the root URL for the hub
        if (baseUrl.EndsWith("/api/v1", StringComparison.OrdinalIgnoreCase))
            baseUrl = baseUrl[..^"/api/v1".Length];
        else if (baseUrl.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
            baseUrl = baseUrl[..^"/api".Length];

        return $"{baseUrl}/hubs/notifications";
    }

    private async Task DisposeConnectionAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await DisposeConnectionAsync();
    }
}
