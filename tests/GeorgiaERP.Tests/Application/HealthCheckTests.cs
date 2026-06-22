using FluentAssertions;
using GeorgiaERP.Infrastructure.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GeorgiaERP.Tests.Application;

public class HealthCheckTests
{
    [Fact]
    public async Task DiskSpaceHealthCheck_ReturnsHealthy_WhenSufficientSpace()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HealthChecks:DiskSpaceThresholdMb"] = "1" // 1 MB threshold - should always have this
            })
            .Build();

        var check = new DiskSpaceHealthCheck(config, NullLogger<DiskSpaceHealthCheck>.Instance);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("disk-space", check, null, null)
        };

        var result = await check.CheckHealthAsync(context);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("MB free");
        result.Data.Should().ContainKey("drive");
        result.Data.Should().ContainKey("freeSpaceMb");
        result.Data.Should().ContainKey("usedPercent");
    }

    [Fact]
    public async Task DiskSpaceHealthCheck_ReturnsDegraded_WhenThresholdExcessive()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HealthChecks:DiskSpaceThresholdMb"] = "99999999" // 99 TB - always exceeds
            })
            .Build();

        var check = new DiskSpaceHealthCheck(config, NullLogger<DiskSpaceHealthCheck>.Instance);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("disk-space", check, null, null)
        };

        var result = await check.CheckHealthAsync(context);

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("Low disk space");
    }

    [Fact]
    public async Task SmtpHealthCheck_ReturnsHealthy_WhenDisabled()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:Smtp:Enabled"] = "false"
            })
            .Build();

        var check = new SmtpHealthCheck(config, NullLogger<SmtpHealthCheck>.Instance);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("smtp", check, null, null)
        };

        var result = await check.CheckHealthAsync(context);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("disabled");
    }

    [Fact]
    public async Task SmtpHealthCheck_ReturnsDegraded_WhenHostNotConfigured()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:Smtp:Enabled"] = "true",
                ["Email:Smtp:Host"] = ""
            })
            .Build();

        var check = new SmtpHealthCheck(config, NullLogger<SmtpHealthCheck>.Instance);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("smtp", check, null, null)
        };

        var result = await check.CheckHealthAsync(context);

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("not configured");
    }

    [Fact]
    public async Task SmtpHealthCheck_ReturnsDegraded_WhenHostUnreachable()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:Smtp:Enabled"] = "true",
                ["Email:Smtp:Host"] = "unreachable.invalid.host.test",
                ["Email:Smtp:Port"] = "25"
            })
            .Build();

        var check = new SmtpHealthCheck(config, NullLogger<SmtpHealthCheck>.Instance);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("smtp", check, null, null)
        };

        var result = await check.CheckHealthAsync(context);

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("Cannot connect");
    }
}
