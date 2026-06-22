using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace GeorgiaERP.Infrastructure.RsGe;

/// <summary>
/// Builds the Polly <see cref="ResiliencePipeline"/> used by
/// <see cref="ResilientRsGeSoapClient"/>. The pipeline layers (inner to outer):
///   1. Per-attempt timeout — cancels a single SOAP call that hangs.
///   2. Retry with exponential backoff + jitter — absorbs transient HTTP failures.
///   3. Circuit breaker — fails fast when RS.GE is persistently unreachable.
///   4. Total timeout — caps the entire operation including all retries.
/// </summary>
public static class RsGeResiliencePipelineFactory
{
    public static ResiliencePipeline Create(RsGeSoapClientResilienceOptions options, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("RsGe.Resilience");

        return new ResiliencePipelineBuilder()
            // 4. Outermost: total timeout across all attempts.
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(options.TotalTimeoutSeconds),
                OnTimeout = args =>
                {
                    logger.LogWarning("RS.GE total timeout ({TotalTimeout}s) exceeded", options.TotalTimeoutSeconds);
                    return default;
                }
            })
            // 3. Circuit breaker: open after N consecutive failures, half-open after duration.
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.8,
                MinimumThroughput = options.CircuitBreakerFailureThreshold,
                SamplingDuration = TimeSpan.FromSeconds(options.CircuitBreakerSamplingWindowSeconds),
                BreakDuration = TimeSpan.FromSeconds(options.CircuitBreakerDurationSeconds),
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutRejectedException>()
                    .Handle<TaskCanceledException>(),
                OnOpened = args =>
                {
                    logger.LogWarning(
                        "RS.GE circuit breaker OPENED for {Duration}s after repeated failures",
                        args.BreakDuration.TotalSeconds);
                    return default;
                },
                OnClosed = args =>
                {
                    logger.LogInformation("RS.GE circuit breaker CLOSED — service recovered");
                    return default;
                },
                OnHalfOpened = args =>
                {
                    logger.LogInformation("RS.GE circuit breaker HALF-OPEN — probing service");
                    return default;
                }
            })
            // 2. Retry: exponential backoff with jitter for transient failures.
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = options.RetryCount,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromSeconds(options.RetryBaseDelaySeconds),
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutRejectedException>()
                    .Handle<TaskCanceledException>(),
                OnRetry = args =>
                {
                    logger.LogWarning(
                        "RS.GE retry attempt {AttemptNumber}/{MaxRetries} after {Delay}ms — {Exception}",
                        args.AttemptNumber + 1,
                        options.RetryCount,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message ?? "unknown");
                    return default;
                }
            })
            // 1. Innermost: per-attempt timeout.
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(options.PerAttemptTimeoutSeconds),
                OnTimeout = args =>
                {
                    logger.LogWarning("RS.GE per-attempt timeout ({PerAttemptTimeout}s) exceeded",
                        options.PerAttemptTimeoutSeconds);
                    return default;
                }
            })
            .Build();
    }
}
