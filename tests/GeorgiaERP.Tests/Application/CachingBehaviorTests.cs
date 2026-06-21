using FluentAssertions;
using GeorgiaERP.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GeorgiaERP.Tests.Application;

public class CachingBehaviorTests
{
    private readonly InMemoryCacheService _cache = new();
    private readonly CachingBehavior<TestCacheableQuery, TestResponse> _behavior;

    public CachingBehaviorTests()
    {
        _behavior = new CachingBehavior<TestCacheableQuery, TestResponse>(
            _cache,
            NullLogger<CachingBehavior<TestCacheableQuery, TestResponse>>.Instance);
    }

    [Fact]
    public async Task CacheMiss_CallsHandler_ThenCachesResult()
    {
        var query = new TestCacheableQuery("key-1");
        var expected = new TestResponse("value-1");
        var handlerCallCount = 0;

        // First call: cache miss, calls handler
        var result = await _behavior.Handle(
            query,
            () =>
            {
                handlerCallCount++;
                return Task.FromResult(expected);
            },
            CancellationToken.None);

        result.Should().Be(expected);
        handlerCallCount.Should().Be(1);

        // Second call: cache hit, handler NOT called
        var result2 = await _behavior.Handle(
            query,
            () =>
            {
                handlerCallCount++;
                return Task.FromResult(new TestResponse("should-not-be-returned"));
            },
            CancellationToken.None);

        result2.Value.Should().Be("value-1");
        handlerCallCount.Should().Be(1, "handler should not be called on cache hit");
    }

    [Fact]
    public async Task DifferentKeys_CacheSeparately()
    {
        var handlerCallCount = 0;

        await _behavior.Handle(
            new TestCacheableQuery("alpha"),
            () =>
            {
                handlerCallCount++;
                return Task.FromResult(new TestResponse("alpha-val"));
            },
            CancellationToken.None);

        await _behavior.Handle(
            new TestCacheableQuery("beta"),
            () =>
            {
                handlerCallCount++;
                return Task.FromResult(new TestResponse("beta-val"));
            },
            CancellationToken.None);

        handlerCallCount.Should().Be(2, "different keys should be separate cache entries");
    }

    [Fact]
    public async Task NonCacheableRequest_AlwaysCallsHandler()
    {
        var behavior = new CachingBehavior<TestNonCacheableQuery, TestResponse>(
            _cache,
            NullLogger<CachingBehavior<TestNonCacheableQuery, TestResponse>>.Instance);

        var handlerCallCount = 0;
        var expected = new TestResponse("direct");

        // Call twice -- handler should be invoked both times
        for (var i = 0; i < 2; i++)
        {
            var result = await behavior.Handle(
                new TestNonCacheableQuery(),
                () =>
                {
                    handlerCallCount++;
                    return Task.FromResult(expected);
                },
                CancellationToken.None);

            result.Should().Be(expected);
        }

        handlerCallCount.Should().Be(2, "non-cacheable requests always hit the handler");
    }

    // --- Cache invalidation behavior tests ---

    [Fact]
    public async Task CacheInvalidation_RemovesCachedKeys_AfterSuccess()
    {
        var invalidationBehavior = new CacheInvalidationBehavior<TestInvalidatingCommand, Result>(
            _cache,
            NullLogger<CacheInvalidationBehavior<TestInvalidatingCommand, Result>>.Instance);

        // Pre-populate cache
        await _cache.SetAsync("products:id:123", new TestResponse("cached-product"));
        await _cache.SetAsync("dashboard:kpi", new TestResponse("cached-dashboard"));

        var command = new TestInvalidatingCommand();

        await invalidationBehavior.Handle(
            command,
            () => Task.FromResult(Result.Success()),
            CancellationToken.None);

        // Verify cache entries were removed
        var product = await _cache.GetAsync<TestResponse>("products:id:123");
        product.Should().BeNull("cache should be invalidated after successful command");

        var dashboard = await _cache.GetAsync<TestResponse>("dashboard:kpi");
        dashboard.Should().BeNull("dashboard cache should be invalidated");
    }

    [Fact]
    public async Task CacheInvalidation_SkipsOnFailure()
    {
        var invalidationBehavior = new CacheInvalidationBehavior<TestInvalidatingCommand, Result>(
            _cache,
            NullLogger<CacheInvalidationBehavior<TestInvalidatingCommand, Result>>.Instance);

        // Pre-populate cache
        await _cache.SetAsync("products:id:123", new TestResponse("cached-product"));

        var command = new TestInvalidatingCommand();

        await invalidationBehavior.Handle(
            command,
            () => Task.FromResult(Result.Failure("not found", "NOT_FOUND")),
            CancellationToken.None);

        // Cache should NOT be invalidated on failure
        var product = await _cache.GetAsync<TestResponse>("products:id:123");
        product.Should().NotBeNull("cache should be preserved when command fails");
    }

    // --- Test types ---

    private record TestCacheableQuery(string Key) : IRequest<TestResponse>, ICacheable
    {
        public string CacheKey => $"test:{Key}";
        public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    }

    private record TestNonCacheableQuery : IRequest<TestResponse>;

    private record TestResponse(string Value);

    private record TestInvalidatingCommand : IRequest<Result>, ICacheInvalidator
    {
        public IReadOnlyList<string> CacheKeysToInvalidate =>
            ["products:id:123", "dashboard:kpi"];
    }

    /// <summary>
    /// Simple in-memory cache for unit testing without Redis.
    /// </summary>
    private sealed class InMemoryCacheService : ICacheService
    {
        private readonly Dictionary<string, object> _store = new();

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            return Task.FromResult(_store.TryGetValue(key, out var value) ? (T)value : null);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null, CancellationToken cancellationToken = default) where T : class
        {
            _store[key] = value!;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _store.Remove(key);
            return Task.CompletedTask;
        }

        public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        {
            var keys = _store.Keys.Where(k => k.StartsWith(prefix)).ToList();
            foreach (var key in keys)
                _store.Remove(key);
            return Task.CompletedTask;
        }
    }
}
