namespace GeorgiaERP.Application.Common;

/// <summary>
/// Marker interface for MediatR requests (commands) that should invalidate
/// cached data after successful execution. Implement on write commands
/// to automatically clear related cache entries via
/// <see cref="CacheInvalidationBehavior{TRequest,TResponse}"/>.
/// </summary>
public interface ICacheInvalidator
{
    /// <summary>
    /// Cache key prefixes to invalidate after this command completes successfully.
    /// Each prefix will be passed to <see cref="ICacheService.RemoveByPrefixAsync"/>,
    /// plus exact key removal for each entry.
    /// </summary>
    IReadOnlyList<string> CacheKeysToInvalidate { get; }
}
