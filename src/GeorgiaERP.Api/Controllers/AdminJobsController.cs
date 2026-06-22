using GeorgiaERP.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeorgiaERP.Api.Controllers;

/// <summary>
/// Admin-only endpoint to view the status of all background services.
/// Shows: job name, last run time, next run time, status, last error message.
/// </summary>
[Authorize(Roles = "super_admin,admin")]
[Tags("Admin")]
[Route("api/v1/admin/jobs")]
public class AdminJobsController : ApiControllerBase
{
    private readonly IBackgroundJobRegistry _jobRegistry;

    public AdminJobsController(IBackgroundJobRegistry jobRegistry)
    {
        _jobRegistry = jobRegistry;
    }

    /// <summary>
    /// Gets the status of all registered background services.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BackgroundJobDto>), StatusCodes.Status200OK)]
    public IActionResult GetAll()
    {
        var jobs = _jobRegistry.GetAll();
        var dtos = jobs.Select(j => new BackgroundJobDto
        {
            JobName = j.JobName,
            Description = j.Description,
            IntervalMinutes = j.Interval.TotalMinutes,
            State = j.State.ToString().ToLowerInvariant(),
            LastRunAt = j.LastRunAt,
            NextRunAt = j.NextRunAt,
            LastError = j.LastError,
            LastErrorAt = j.LastErrorAt,
            TotalRuns = j.TotalRuns,
            TotalFailures = j.TotalFailures,
            RegisteredAt = j.RegisteredAt
        }).ToList();

        return Ok(dtos);
    }

    /// <summary>
    /// Gets the status of a specific background service by name.
    /// </summary>
    [HttpGet("{jobName}")]
    [ProducesResponseType(typeof(BackgroundJobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Get(string jobName)
    {
        var job = _jobRegistry.Get(jobName);
        if (job is null)
            return NotFound(new { error = $"Job '{jobName}' not found" });

        return Ok(new BackgroundJobDto
        {
            JobName = job.JobName,
            Description = job.Description,
            IntervalMinutes = job.Interval.TotalMinutes,
            State = job.State.ToString().ToLowerInvariant(),
            LastRunAt = job.LastRunAt,
            NextRunAt = job.NextRunAt,
            LastError = job.LastError,
            LastErrorAt = job.LastErrorAt,
            TotalRuns = job.TotalRuns,
            TotalFailures = job.TotalFailures,
            RegisteredAt = job.RegisteredAt
        });
    }
}

/// <summary>
/// DTO for background job status.
/// </summary>
public sealed class BackgroundJobDto
{
    public string JobName { get; init; } = default!;
    public string Description { get; init; } = default!;
    public double IntervalMinutes { get; init; }
    public string State { get; init; } = default!;
    public DateTimeOffset? LastRunAt { get; init; }
    public DateTimeOffset? NextRunAt { get; init; }
    public string? LastError { get; init; }
    public DateTimeOffset? LastErrorAt { get; init; }
    public long TotalRuns { get; init; }
    public long TotalFailures { get; init; }
    public DateTimeOffset RegisteredAt { get; init; }
}
