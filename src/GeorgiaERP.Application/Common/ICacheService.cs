namespace GeorgiaERP.Application.Common;

/// <summary>
/// Abstraction over distributed caching (Redis) used across the application.
/// All methods are async-first and safe for concurrent access.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a cached value by key, deserializing from JSON.
    /// Returns null if the key does not exist or has expired.
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Sets a value in the cache with the specified absolute expiration.
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Removes a cached value by key.
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all keys matching the specified prefix pattern.
    /// </summary>
    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
}
