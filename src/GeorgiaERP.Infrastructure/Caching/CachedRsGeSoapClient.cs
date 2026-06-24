using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Compliance;

namespace GeorgiaERP.Infrastructure.Caching;

/// <summary>
/// Decorator that caches read-only RS.GE SOAP responses in Redis.
/// Write operations (save/send/confirm/close/reject) are never cached.
/// TIN lookups are cached for 24h since company registration data rarely changes.
/// Reference data (units, transport types, waybill types) is cached for 1h.
/// </summary>
public sealed class CachedRsGeSoapClient : IRsGeSoapClient
{
    private readonly IRsGeSoapClient _inner;
    private readonly ICacheService _cache;

    public CachedRsGeSoapClient(IRsGeSoapClient inner, ICacheService cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public Task<string> GetMyIpAsync() => _inner.GetMyIpAsync();

    public Task<RsGeServiceUser> CheckServiceUserAsync(string serviceUser, string servicePassword)
        => _inner.CheckServiceUserAsync(serviceUser, servicePassword);

    public async Task<RsGeNameResult> GetNameFromTinAsync(string tin)
    {
        var cacheKey = CacheKeys.RsGeTinName + tin;
        var cached = await _cache.GetAsync<RsGeNameResult>(cacheKey).ConfigureAwait(false);
        if (cached is not null)
            return cached;

        var result = await _inner.GetNameFromTinAsync(tin).ConfigureAwait(false);
        if (result.Found)
            await _cache.SetAsync(cacheKey, result, CacheKeys.TinValidationTtl).ConfigureAwait(false);

        return result;
    }

    public async Task<bool> IsVatPayerAsync(string tin)
    {
        var cacheKey = CacheKeys.RsGeTinVat + tin;
        var cached = await _cache.GetAsync<VatPayerCacheEntry>(cacheKey).ConfigureAwait(false);
        if (cached is not null)
            return cached.IsVatPayer;

        var result = await _inner.IsVatPayerAsync(tin).ConfigureAwait(false);
        await _cache.SetAsync(cacheKey, new VatPayerCacheEntry(result), CacheKeys.TinValidationTtl).ConfigureAwait(false);
        return result;
    }

    public async Task<IReadOnlyList<RsGeUnit>> GetUnitsAsync()
    {
        var cached = await _cache.GetAsync<List<RsGeUnit>>(CacheKeys.RsGeUnits).ConfigureAwait(false);
        if (cached is not null)
            return cached;

        var result = await _inner.GetUnitsAsync().ConfigureAwait(false);
        await _cache.SetAsync(CacheKeys.RsGeUnits, result.ToList(), CacheKeys.LongTtl).ConfigureAwait(false);
        return result;
    }

    public async Task<IReadOnlyList<RsGeTransportType>> GetTransportTypesAsync()
    {
        var cached = await _cache.GetAsync<List<RsGeTransportType>>(CacheKeys.RsGeTransportTypes).ConfigureAwait(false);
        if (cached is not null)
            return cached;

        var result = await _inner.GetTransportTypesAsync().ConfigureAwait(false);
        await _cache.SetAsync(CacheKeys.RsGeTransportTypes, result.ToList(), CacheKeys.LongTtl).ConfigureAwait(false);
        return result;
    }

    public async Task<IReadOnlyList<RsGeWaybillType>> GetWaybillTypesAsync()
    {
        var cached = await _cache.GetAsync<List<RsGeWaybillType>>(CacheKeys.RsGeWaybillTypes).ConfigureAwait(false);
        if (cached is not null)
            return cached;

        var result = await _inner.GetWaybillTypesAsync().ConfigureAwait(false);
        await _cache.SetAsync(CacheKeys.RsGeWaybillTypes, result.ToList(), CacheKeys.LongTtl).ConfigureAwait(false);
        return result;
    }

    // Write operations -- never cached
    public Task<RsGeWaybillResult> SaveWaybillAsync(RsGeWaybillRequest request) => _inner.SaveWaybillAsync(request);
    public Task<RsGeResult> SendWaybillAsync(int waybillId) => _inner.SendWaybillAsync(waybillId);
    public Task<RsGeResult> ConfirmWaybillAsync(int waybillId) => _inner.ConfirmWaybillAsync(waybillId);
    public Task<RsGeResult> CloseWaybillAsync(int waybillId) => _inner.CloseWaybillAsync(waybillId);
    public Task<RsGeResult> RejectWaybillAsync(int waybillId) => _inner.RejectWaybillAsync(waybillId);
    public Task<RsGeWaybillData?> GetWaybillAsync(int waybillId) => _inner.GetWaybillAsync(waybillId);
    public Task<RsGeResult> SaveInvoiceAsync(RsGeInvoiceRequest request) => _inner.SaveInvoiceAsync(request);
    public Task<RsGeResult> SubmitVatDeclarationAsync(RsGeVatDeclarationRequest request) => _inner.SubmitVatDeclarationAsync(request);

    /// <summary>Wrapper to cache a boolean value since JSON needs an object.</summary>
    private sealed record VatPayerCacheEntry(bool IsVatPayer);
}
