using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;

namespace GeorgiaERP.Desktop.Services;

public interface IOfflineQueueService
{
    int PendingCount { get; }
    void Enqueue(OfflineOperation operation);
    Task<int> FlushAsync(CancellationToken ct = default);
    event Action? QueueChanged;
}

public record OfflineOperation(string Method, string Endpoint, string? JsonBody, DateTimeOffset CreatedAt);

public class OfflineQueueService : IOfflineQueueService
{
    private readonly ConcurrentQueue<OfflineOperation> _queue = new();
    private readonly IApiClient _apiClient;
    private readonly string _persistPath;

    public int PendingCount => _queue.Count;
    public event Action? QueueChanged;

    public OfflineQueueService(IApiClient apiClient)
    {
        _apiClient = apiClient;
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "GeorgiaERP");
        Directory.CreateDirectory(dir);
        _persistPath = Path.Combine(dir, "offline_queue.json");
        Load();
    }

    public void Enqueue(OfflineOperation operation)
    {
        _queue.Enqueue(operation);
        Save();
        QueueChanged?.Invoke();
    }

    public async Task<int> FlushAsync(CancellationToken ct = default)
    {
        var processed = 0;
        while (_queue.TryPeek(out var op))
        {
            try
            {
                var result = op.Method.ToUpperInvariant() switch
                {
                    "POST" when op.JsonBody is not null =>
                        await _apiClient.PostAsync<object>(op.Endpoint, JsonSerializer.Deserialize<object>(op.JsonBody)!, ct),
                    "POST" => await _apiClient.PostAsync(op.Endpoint, ct),
                    _ => new Models.ApiResult { IsSuccess = true }
                };

                if (!result.IsSuccess) break;

                _queue.TryDequeue(out _);
                processed++;
            }
            catch
            {
                break;
            }
        }

        if (processed > 0)
        {
            Save();
            QueueChanged?.Invoke();
        }

        return processed;
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_queue.ToArray());
            File.WriteAllText(_persistPath, json);
        }
        catch { }
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_persistPath)) return;
            var json = File.ReadAllText(_persistPath);
            var items = JsonSerializer.Deserialize<OfflineOperation[]>(json);
            if (items is null) return;
            foreach (var item in items) _queue.Enqueue(item);
        }
        catch { }
    }
}
