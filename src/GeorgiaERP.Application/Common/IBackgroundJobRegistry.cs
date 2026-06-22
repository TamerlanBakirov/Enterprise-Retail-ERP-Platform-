namespace GeorgiaERP.Application.Common;

/// <summary>
/// Registry for background services to report their status.
/// Services register on startup and update their status on each execution cycle.
/// </summary>
public interface IBackgroundJobRegistry
{
    /// <summary>
    /// Registers a background job with the registry.
    /// </summary>
    void Register(string jobName, string description, TimeSpan interval);

    /// <summary>
    /// Records a successful execution of the job.
    /// </summary>
    void RecordSuccess(string jobName);

    /// <summary>
    /// Records a failed execution of the job.
    /// </summary>
    void RecordFailure(string jobName, string errorMessage);

    /// <summary>
    /// Marks the job as currently running.
    /// </summary>
    void MarkRunning(string jobName);

    /// <summary>
    /// Marks the job as idle (finished current cycle).
    /// </summary>
    void MarkIdle(string jobName);

    /// <summary>
    /// Gets the status of all registered jobs.
    /// </summary>
    IReadOnlyList<BackgroundJobStatus> GetAll();

    /// <summary>
    /// Gets the status of a specific job by name.
    /// </summary>
    BackgroundJobStatus? Get(string jobName);
}

/// <summary>
/// Represents the current status of a background job.
/// </summary>
public sealed class BackgroundJobStatus
{
    public required string JobName { get; init; }
    public required string Description { get; init; }
    public required TimeSpan Interval { get; init; }
    public required BackgroundJobState State { get; init; }
    public DateTimeOffset? LastRunAt { get; init; }
    public DateTimeOffset? NextRunAt { get; init; }
    public string? LastError { get; init; }
    public DateTimeOffset? LastErrorAt { get; init; }
    public long TotalRuns { get; init; }
    public long TotalFailures { get; init; }
    public DateTimeOffset RegisteredAt { get; init; }
}

/// <summary>
/// Possible states for a background job.
/// </summary>
public enum BackgroundJobState
{
    Registered,
    Running,
    Idle,
    Error
}
