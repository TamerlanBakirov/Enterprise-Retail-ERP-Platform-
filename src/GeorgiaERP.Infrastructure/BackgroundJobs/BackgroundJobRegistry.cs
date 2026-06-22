using System.Collections.Concurrent;
using GeorgiaERP.Application.Common;

namespace GeorgiaERP.Infrastructure.BackgroundJobs;

/// <summary>
/// Thread-safe in-memory registry for background job status tracking.
/// Registered as a singleton so all background services share the same instance.
/// </summary>
public sealed class BackgroundJobRegistry : IBackgroundJobRegistry
{
    private readonly ConcurrentDictionary<string, JobEntry> _jobs = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string jobName, string description, TimeSpan interval)
    {
        _jobs.AddOrUpdate(jobName,
            _ => new JobEntry
            {
                JobName = jobName,
                Description = description,
                Interval = interval,
                State = BackgroundJobState.Registered,
                RegisteredAt = DateTimeOffset.UtcNow
            },
            (_, existing) =>
            {
                existing.Description = description;
                existing.Interval = interval;
                return existing;
            });
    }

    public void RecordSuccess(string jobName)
    {
        if (_jobs.TryGetValue(jobName, out var entry))
        {
            lock (entry)
            {
                entry.LastRunAt = DateTimeOffset.UtcNow;
                entry.NextRunAt = DateTimeOffset.UtcNow.Add(entry.Interval);
                entry.State = BackgroundJobState.Idle;
                entry.TotalRuns++;
            }
        }
    }

    public void RecordFailure(string jobName, string errorMessage)
    {
        if (_jobs.TryGetValue(jobName, out var entry))
        {
            lock (entry)
            {
                entry.LastRunAt = DateTimeOffset.UtcNow;
                entry.NextRunAt = DateTimeOffset.UtcNow.Add(entry.Interval);
                entry.State = BackgroundJobState.Error;
                entry.LastError = errorMessage;
                entry.LastErrorAt = DateTimeOffset.UtcNow;
                entry.TotalRuns++;
                entry.TotalFailures++;
            }
        }
    }

    public void MarkRunning(string jobName)
    {
        if (_jobs.TryGetValue(jobName, out var entry))
        {
            lock (entry)
            {
                entry.State = BackgroundJobState.Running;
            }
        }
    }

    public void MarkIdle(string jobName)
    {
        if (_jobs.TryGetValue(jobName, out var entry))
        {
            lock (entry)
            {
                entry.State = BackgroundJobState.Idle;
            }
        }
    }

    public IReadOnlyList<BackgroundJobStatus> GetAll()
    {
        return _jobs.Values.Select(ToStatus).OrderBy(s => s.JobName).ToList();
    }

    public BackgroundJobStatus? Get(string jobName)
    {
        return _jobs.TryGetValue(jobName, out var entry) ? ToStatus(entry) : null;
    }

    private static BackgroundJobStatus ToStatus(JobEntry entry)
    {
        lock (entry)
        {
            return new BackgroundJobStatus
            {
                JobName = entry.JobName,
                Description = entry.Description,
                Interval = entry.Interval,
                State = entry.State,
                LastRunAt = entry.LastRunAt,
                NextRunAt = entry.NextRunAt,
                LastError = entry.LastError,
                LastErrorAt = entry.LastErrorAt,
                TotalRuns = entry.TotalRuns,
                TotalFailures = entry.TotalFailures,
                RegisteredAt = entry.RegisteredAt
            };
        }
    }

    private sealed class JobEntry
    {
        public string JobName { get; set; } = default!;
        public string Description { get; set; } = default!;
        public TimeSpan Interval { get; set; }
        public BackgroundJobState State { get; set; }
        public DateTimeOffset? LastRunAt { get; set; }
        public DateTimeOffset? NextRunAt { get; set; }
        public string? LastError { get; set; }
        public DateTimeOffset? LastErrorAt { get; set; }
        public long TotalRuns { get; set; }
        public long TotalFailures { get; set; }
        public DateTimeOffset RegisteredAt { get; set; }
    }
}
