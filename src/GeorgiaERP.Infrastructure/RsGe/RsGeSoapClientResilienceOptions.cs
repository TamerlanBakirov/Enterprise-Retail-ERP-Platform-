namespace GeorgiaERP.Infrastructure.RsGe;

/// <summary>
/// Configuration for Polly resilience policies applied to the RS.GE SOAP client.
/// Bound from the "RsGe:Resilience" configuration section.
/// </summary>
public class RsGeSoapClientResilienceOptions
{
    public const string SectionName = "RsGe:Resilience";

    /// <summary>Number of retry attempts for transient HTTP failures.</summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>Median delay (seconds) for the first retry; subsequent retries use exponential backoff with jitter.</summary>
    public int RetryBaseDelaySeconds { get; set; } = 2;

    /// <summary>Number of consecutive failures before the circuit breaker opens.</summary>
    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    /// <summary>Duration (seconds) the circuit stays open before allowing a probe request.</summary>
    public int CircuitBreakerDurationSeconds { get; set; } = 30;

    /// <summary>Sampling window (seconds) for the circuit breaker failure rate calculation.</summary>
    public int CircuitBreakerSamplingWindowSeconds { get; set; } = 60;

    /// <summary>Overall timeout (seconds) for a single SOAP request including retries.</summary>
    public int TotalTimeoutSeconds { get; set; } = 60;

    /// <summary>Per-attempt timeout (seconds). Should be less than TotalTimeoutSeconds.</summary>
    public int PerAttemptTimeoutSeconds { get; set; } = 15;
}
