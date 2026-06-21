using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace GeorgiaERP.Infrastructure.Messaging;

/// <summary>
/// Redis-backed message deduplication using distributed cache. Each processed
/// message ID is stored with a TTL so the deduplication window is bounded.
/// The TTL matches the queue message TTL (24h) to cover the full lifecycle of
/// a message in the system.
/// </summary>
public sealed class RedisMessageDeduplicator : IMessageDeduplicator
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisMessageDeduplicator> _logger;

    private const string KeyPrefix = "rsge:dedup:";
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(25) // slightly longer than queue TTL
    };

    public RedisMessageDeduplicator(IDistributedCache cache, ILogger<RedisMessageDeduplicator> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> IsDuplicateAsync(string messageId, CancellationToken cancellationToken = default)
    {
        var key = KeyPrefix + messageId;

        try
        {
            var existing = await _cache.GetAsync(key, cancellationToken);
            if (existing is not null)
            {
                _logger.LogInformation("Duplicate message detected: {MessageId}", messageId);
                return true;
            }

            // Mark as processed. In Redis this is atomic per key.
            await _cache.SetAsync(key, "1"u8.ToArray(), CacheOptions, cancellationToken);
            return false;
        }
        catch (Exception ex)
        {
            // If Redis is unavailable, allow the message through — better to process a
            // duplicate than to lose a message. The submission processor has its own
            // idempotency guard (checking terminal document status).
            _logger.LogWarning(ex, "Deduplication check failed for {MessageId}; allowing message through", messageId);
            return false;
        }
    }
}
