using System.Text.Json;
using System.Threading;
using GeorgiaERP.Application.Common;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace GeorgiaERP.Infrastructure.Caching;

/// <summary>
/// Redis-backed implementation of <see cref="ICacheService"/> using IDistributedCache.
/// Falls back gracefully when Redis is unavailable -- cache misses return null,
/// writes are silently dropped, so the application keeps working without Redis.
///
/// A lightweight circuit breaker prevents the connect-timeout penalty (several
/// seconds per call) from being paid on every request when Redis is down: after
/// one failure the cache is bypassed entirely for a short cooldown.
/// </summary>
public sealed class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static readonly TimeSpan CircuitCooldown = TimeSpan.FromSeconds(30);
    private static long _circuitOpenUntilTicks; // UTC ticks; cache bypassed while now < this

    private static bool CircuitOpen => Volatile.Read(ref _circuitOpenUntilTicks) > DateTime.UtcNow.Ticks;

    private void TripCircuit(string op, Exception ex)
    {
        Volatile.Write(ref _circuitOpenUntilTicks, DateTime.UtcNow.Add(CircuitCooldown).Ticks);
        _logger.LogWarning(ex, "Redis cache {Op} failed; bypassing cache for {Seconds}s.", op, CircuitCooldown.TotalSeconds);
    }

    public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (CircuitOpen) return null;
        try
        {
            var bytes = await _cache.GetAsync(key, cancellationToken).ConfigureAwait(false);
            if (bytes is null || bytes.Length == 0)
                return null;

            return JsonSerializer.Deserialize<T>(bytes, JsonOptions);
        }
        catch (Exception ex)
        {
            TripCircuit("GET", ex);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null, CancellationToken cancellationToken = default) where T : class
    {
        if (CircuitOpen) return;
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = absoluteExpiration ?? TimeSpan.FromMinutes(5)
            };

            await _cache.SetAsync(key, bytes, options, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            TripCircuit("SET", ex);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (CircuitOpen) return;
        try
        {
            await _cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            TripCircuit("REMOVE", ex);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        // IDistributedCache does not support pattern-based deletion natively.
        // For production, this would use StackExchange.Redis IServer.Keys() directly.
        // Here we log the intent; callers should use specific key invalidation where possible.
        _logger.LogDebug("RemoveByPrefix requested for prefix {Prefix}. " +
                         "Use explicit key removal for IDistributedCache backends.", prefix);
        await Task.CompletedTask;
    }
}
