using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace GeorgiaERP.Infrastructure.HealthChecks;

/// <summary>
/// Monitors available disk space on the application's drive. Reports degraded
/// when free space drops below a configurable threshold (default: 1 GB).
/// </summary>
public class DiskSpaceHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DiskSpaceHealthCheck> _logger;

    public DiskSpaceHealthCheck(IConfiguration configuration, ILogger<DiskSpaceHealthCheck> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var thresholdMb = _configuration.GetValue("HealthChecks:DiskSpaceThresholdMb", 1024L);
            var thresholdBytes = thresholdMb * 1024 * 1024;

            var appPath = AppContext.BaseDirectory;
            var driveInfo = new DriveInfo(Path.GetPathRoot(appPath) ?? appPath);

            if (!driveInfo.IsReady)
            {
                return Task.FromResult(HealthCheckResult.Degraded("Drive is not ready."));
            }

            var freeSpaceMb = driveInfo.AvailableFreeSpace / (1024.0 * 1024.0);
            var totalSpaceMb = driveInfo.TotalSize / (1024.0 * 1024.0);
            var usedPercent = (1 - (double)driveInfo.AvailableFreeSpace / driveInfo.TotalSize) * 100;

            var data = new Dictionary<string, object>
            {
                ["drive"] = driveInfo.Name,
                ["freeSpaceMb"] = Math.Round(freeSpaceMb, 1),
                ["totalSpaceMb"] = Math.Round(totalSpaceMb, 1),
                ["usedPercent"] = Math.Round(usedPercent, 1),
                ["thresholdMb"] = thresholdMb
            };

            if (driveInfo.AvailableFreeSpace < thresholdBytes)
            {
                _logger.LogWarning(
                    "Low disk space on {Drive}: {FreeMb:F0} MB free ({UsedPercent:F1}% used), threshold: {ThresholdMb} MB",
                    driveInfo.Name, freeSpaceMb, usedPercent, thresholdMb);

                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Low disk space: {freeSpaceMb:F0} MB free on {driveInfo.Name} ({usedPercent:F1}% used).",
                    data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"Disk space OK: {freeSpaceMb:F0} MB free on {driveInfo.Name} ({usedPercent:F1}% used).",
                data: data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Disk space health check failed");
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Unable to check disk space: {ex.Message}"));
        }
    }
}
