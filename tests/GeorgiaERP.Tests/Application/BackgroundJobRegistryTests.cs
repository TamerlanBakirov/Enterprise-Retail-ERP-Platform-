using GeorgiaERP.Application.Common;
using GeorgiaERP.Infrastructure.BackgroundJobs;
using FluentAssertions;
using Xunit;

namespace GeorgiaERP.Tests.Application;

public class BackgroundJobRegistryTests
{
    private readonly BackgroundJobRegistry _sut = new();

    [Fact]
    public void Register_AddsJobToRegistry()
    {
        _sut.Register("TestJob", "A test job", TimeSpan.FromMinutes(5));

        var job = _sut.Get("TestJob");
        job.Should().NotBeNull();
        job!.JobName.Should().Be("TestJob");
        job.Description.Should().Be("A test job");
        job.Interval.Should().Be(TimeSpan.FromMinutes(5));
        job.State.Should().Be(BackgroundJobState.Registered);
    }

    [Fact]
    public void GetAll_ReturnsAllRegisteredJobs()
    {
        _sut.Register("Job1", "First job", TimeSpan.FromMinutes(5));
        _sut.Register("Job2", "Second job", TimeSpan.FromMinutes(10));
        _sut.Register("Job3", "Third job", TimeSpan.FromHours(1));

        var jobs = _sut.GetAll();
        jobs.Should().HaveCount(3);
        jobs.Select(j => j.JobName).Should().BeEquivalentTo("Job1", "Job2", "Job3");
    }

    [Fact]
    public void Get_ReturnsNull_WhenJobNotRegistered()
    {
        var job = _sut.Get("NonExistentJob");
        job.Should().BeNull();
    }

    [Fact]
    public void RecordSuccess_UpdatesStatusCorrectly()
    {
        _sut.Register("TestJob", "A test job", TimeSpan.FromMinutes(5));

        _sut.RecordSuccess("TestJob");

        var job = _sut.Get("TestJob");
        job.Should().NotBeNull();
        job!.State.Should().Be(BackgroundJobState.Idle);
        job.TotalRuns.Should().Be(1);
        job.TotalFailures.Should().Be(0);
        job.LastRunAt.Should().NotBeNull();
        job.NextRunAt.Should().NotBeNull();
        job.LastError.Should().BeNull();
    }

    [Fact]
    public void RecordFailure_UpdatesStatusAndError()
    {
        _sut.Register("TestJob", "A test job", TimeSpan.FromMinutes(5));

        _sut.RecordFailure("TestJob", "Something went wrong");

        var job = _sut.Get("TestJob");
        job.Should().NotBeNull();
        job!.State.Should().Be(BackgroundJobState.Error);
        job.TotalRuns.Should().Be(1);
        job.TotalFailures.Should().Be(1);
        job.LastError.Should().Be("Something went wrong");
        job.LastErrorAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkRunning_SetsStateToRunning()
    {
        _sut.Register("TestJob", "A test job", TimeSpan.FromMinutes(5));

        _sut.MarkRunning("TestJob");

        var job = _sut.Get("TestJob");
        job!.State.Should().Be(BackgroundJobState.Running);
    }

    [Fact]
    public void MarkIdle_SetsStateToIdle()
    {
        _sut.Register("TestJob", "A test job", TimeSpan.FromMinutes(5));
        _sut.MarkRunning("TestJob");

        _sut.MarkIdle("TestJob");

        var job = _sut.Get("TestJob");
        job!.State.Should().Be(BackgroundJobState.Idle);
    }

    [Fact]
    public void MultipleSuccessesAndFailures_TrackCumulativeCounts()
    {
        _sut.Register("TestJob", "A test job", TimeSpan.FromMinutes(5));

        _sut.RecordSuccess("TestJob");
        _sut.RecordSuccess("TestJob");
        _sut.RecordFailure("TestJob", "Error 1");
        _sut.RecordSuccess("TestJob");
        _sut.RecordFailure("TestJob", "Error 2");

        var job = _sut.Get("TestJob");
        job!.TotalRuns.Should().Be(5);
        job.TotalFailures.Should().Be(2);
        job.LastError.Should().Be("Error 2");
    }

    [Fact]
    public void Get_IsCaseInsensitive()
    {
        _sut.Register("TestJob", "A test job", TimeSpan.FromMinutes(5));

        var job = _sut.Get("testjob");
        job.Should().NotBeNull();
        job!.JobName.Should().Be("TestJob");
    }

    [Fact]
    public void Register_UpdatesExistingJob()
    {
        _sut.Register("TestJob", "Original description", TimeSpan.FromMinutes(5));
        _sut.Register("TestJob", "Updated description", TimeSpan.FromMinutes(10));

        var jobs = _sut.GetAll();
        jobs.Should().HaveCount(1);

        var job = jobs[0];
        job.Description.Should().Be("Updated description");
        job.Interval.Should().Be(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public void RecordSuccess_CalculatesNextRunTime()
    {
        var interval = TimeSpan.FromMinutes(30);
        _sut.Register("TestJob", "A test job", interval);

        var beforeRecord = DateTimeOffset.UtcNow;
        _sut.RecordSuccess("TestJob");

        var job = _sut.Get("TestJob");
        job!.NextRunAt.Should().NotBeNull();
        // Next run should be approximately interval from now
        job.NextRunAt!.Value.Should().BeOnOrAfter(beforeRecord.Add(interval).AddSeconds(-1));
    }

    [Fact]
    public void Operations_OnUnregisteredJob_AreNoOps()
    {
        // These should not throw
        _sut.RecordSuccess("NoSuchJob");
        _sut.RecordFailure("NoSuchJob", "error");
        _sut.MarkRunning("NoSuchJob");
        _sut.MarkIdle("NoSuchJob");

        _sut.GetAll().Should().BeEmpty();
    }

    [Fact]
    public void GetAll_ReturnsSortedByJobName()
    {
        _sut.Register("Zebra", "Z job", TimeSpan.FromMinutes(1));
        _sut.Register("Alpha", "A job", TimeSpan.FromMinutes(1));
        _sut.Register("Middle", "M job", TimeSpan.FromMinutes(1));

        var jobs = _sut.GetAll();
        jobs.Select(j => j.JobName).Should().BeInAscendingOrder();
    }
}
