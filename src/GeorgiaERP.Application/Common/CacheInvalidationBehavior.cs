using MediatR;
using Microsoft.Extensions.Logging;

namespace GeorgiaERP.Application.Common;

/// <summary>
/// MediatR pipeline behavior that invalidates cache entries after any
/// command implementing <see cref="ICacheInvalidator"/> completes successfully.
/// Runs after the handler, not before, to avoid clearing cache for failed operations.
/// </summary>
public sealed class CacheInvalidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICacheService _cache;
    private readonly ILogger<CacheInvalidationBehavior<TRequest, TResponse>> _logger;

    public CacheInvalidationBehavior(ICacheService cache, ILogger<CacheInvalidationBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        if (request is not ICacheInvalidator invalidator)
            return response;

        // Only invalidate on successful Result outcomes
        if (response is Result result && !result.IsSuccess)
            return response;

        foreach (var key in invalidator.CacheKeysToInvalidate)
        {
            _logger.LogDebug("Invalidating cache key: {CacheKey}", key);
            await _cache.RemoveAsync(key, cancellationToken);
        }

        return response;
    }
}
