namespace GeorgiaERP.Application.Common;

/// <summary>
/// Marker interface for MediatR requests that should be cached.
/// Implement this on query records to enable automatic cache-aside behavior
/// via the <see cref="CachingBehavior{TRequest,TResponse}"/> pipeline.
/// </summary>
public interface ICacheable
{
    /// <summary>
    /// Cache key for this specific request instance. Must be unique per parameter combination.
    /// </summary>
    string CacheKey { get; }

    /// <summary>
    /// How long the cached value should live. Defaults to 2 minutes if not overridden.
    /// </summary>
    TimeSpan? CacheDuration => null;
}
