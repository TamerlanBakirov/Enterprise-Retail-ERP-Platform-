using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GeorgiaERP.Application.Common;

/// <summary>
/// MediatR pipeline behavior that implements cache-aside for any request
/// implementing <see cref="ICacheable"/>. On cache hit, the handler is skipped
/// entirely. On miss, the handler result is cached with the specified TTL.
///
/// Gracefully degrades when cache is unavailable (Redis down) -- misses just
/// fall through to the handler, matching RedisCacheService behavior.
/// </summary>
public sealed class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICacheService _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(2);

    public CachingBehavior(ICacheService cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only cache requests that opt in via ICacheable
        if (request is not ICacheable cacheable)
            return await next();

        var cacheKey = cacheable.CacheKey;

        // Try cache first -- ICacheService.GetAsync requires T : class.
        // TResponse is not constrained, so we use a wrapper approach:
        // serialize to CacheWrapper<TResponse> which is always a class.
        var cached = await _cache.GetAsync<CacheWrapper<TResponse>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            _logger.LogDebug("Cache HIT for {CacheKey}", cacheKey);
            return cached.Value;
        }

        _logger.LogDebug("Cache MISS for {CacheKey}, executing handler", cacheKey);

        var response = await next();

        // Cache the result
        if (response is not null)
        {
            var ttl = cacheable.CacheDuration ?? DefaultTtl;
            var wrapper = new CacheWrapper<TResponse> { Value = response };
            await _cache.SetAsync(cacheKey, wrapper, ttl, cancellationToken);
        }

        return response;
    }

    /// <summary>
    /// Wrapper class to satisfy the <c>where T : class</c> constraint on ICacheService.
    /// This allows caching of value-type responses as well.
    /// </summary>
    private sealed class CacheWrapper<T>
    {
        public T Value { get; init; } = default!;
    }
}
