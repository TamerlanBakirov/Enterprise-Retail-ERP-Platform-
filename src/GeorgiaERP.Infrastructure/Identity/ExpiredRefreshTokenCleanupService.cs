using GeorgiaERP.Application.Common;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace GeorgiaERP.Infrastructure.Identity;

/// <summary>
/// Background service that periodically removes expired and revoked refresh tokens
/// from the database. This prevents the RefreshTokens table from growing unboundedly,
/// improving query performance and reducing storage costs.
///
/// Cleanup policy:
///   - Removes tokens whose ExpiresAt has passed.
///   - Removes tokens that were revoked more than a configurable retention period ago
///     (default 7 days), preserving recent revocations for audit/forensics.
///
/// Runs once per hour by default. Configurable via appsettings:
///   "RefreshTokenCleanup": {
///     "IntervalMinutes": 60,
///     "RevokedRetentionDays": 7,
///     "BatchSize": 1000
///   }
/// </summary>
public class ExpiredRefreshTokenCleanupService : BackgroundService
{
    private const string JobName = "ExpiredRefreshTokenCleanup";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiredRefreshTokenCleanupService> _logger;
    private readonly IBackgroundJobRegistry _jobRegistry;
    private readonly TimeSpan _interval;
    private readonly int _revokedRetentionDays;
    private readonly int _batchSize;

    public ExpiredRefreshTokenCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExpiredRefreshTokenCleanupService> logger,
        IConfiguration configuration,
        IBackgroundJobRegistry jobRegistry)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _jobRegistry = jobRegistry;

        var section = configuration.GetSection("RefreshTokenCleanup");
        _interval = TimeSpan.FromMinutes(section.GetValue("IntervalMinutes", 60));
        _revokedRetentionDays = section.GetValue("RevokedRetentionDays", 7);
        _batchSize = section.GetValue("BatchSize", 1000);

        _jobRegistry.Register(JobName,
            "Removes expired and revoked refresh tokens from the database",
            _interval);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Stagger startup to let the application stabilize before running cleanup.
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        _logger.LogInformation(
            "Refresh token cleanup service started. Interval: {IntervalMinutes}min, " +
            "Revoked retention: {RetentionDays}d, Batch size: {BatchSize}",
            _interval.TotalMinutes, _revokedRetentionDays, _batchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _jobRegistry.MarkRunning(JobName);
                var removed = await CleanupExpiredTokensAsync(stoppingToken);
                _jobRegistry.RecordSuccess(JobName);
                if (removed > 0)
                {
                    _logger.LogInformation(
                        "Refresh token cleanup completed: {RemovedCount} tokens removed", removed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refresh token cleanup failed");
                _jobRegistry.RecordFailure(JobName, ex.Message);
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task<int> CleanupExpiredTokensAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTimeOffset.UtcNow;
        var revokedCutoff = now.AddDays(-_revokedRetentionDays);
        var totalRemoved = 0;

        // Process in batches to avoid long-running transactions and excessive memory usage.
        while (true)
        {
            var tokensToRemove = await dbContext.RefreshTokens
                .Where(t =>
                    t.ExpiresAt < now ||
                    (t.RevokedAt != null && t.RevokedAt < revokedCutoff))
                .OrderBy(t => t.ExpiresAt)
                .Take(_batchSize)
                .ToListAsync(cancellationToken);

            if (tokensToRemove.Count == 0)
                break;

            dbContext.RefreshTokens.RemoveRange(tokensToRemove);
            await dbContext.SaveChangesAsync(cancellationToken);
            totalRemoved += tokensToRemove.Count;

            _logger.LogDebug(
                "Removed batch of {BatchCount} expired/revoked refresh tokens", tokensToRemove.Count);

            // If the batch was smaller than the limit, we've processed everything.
            if (tokensToRemove.Count < _batchSize)
                break;
        }

        return totalRemoved;
    }
}
