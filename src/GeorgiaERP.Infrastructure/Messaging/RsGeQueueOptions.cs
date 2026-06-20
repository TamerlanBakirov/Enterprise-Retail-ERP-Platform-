namespace GeorgiaERP.Infrastructure.Messaging;

/// <summary>
/// Bound from the "RsGe:Queue" configuration section. Controls broker connection
/// and the retry/backoff policy applied by the consumer before dead-lettering.
/// </summary>
public class RsGeQueueOptions
{
    public const string SectionName = "RsGe:Queue";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";

    /// <summary>Maximum delivery attempts before a message is dead-lettered.</summary>
    public int MaxRetryCount { get; set; } = 10;

    /// <summary>Base delay (seconds) for exponential backoff between retries.</summary>
    public int RetryBaseDelaySeconds { get; set; } = 5;

    /// <summary>Upper bound (seconds) on a single backoff delay.</summary>
    public int RetryMaxDelaySeconds { get; set; } = 300;
}
