using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GeorgiaERP.Infrastructure.HealthChecks;

/// <summary>
/// Extension methods for registering RS.GE-specific health checks.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Adds health checks for RS.GE SOAP connectivity and RabbitMQ queue monitoring.
    /// </summary>
    public static IHealthChecksBuilder AddRsGeHealthChecks(this IHealthChecksBuilder builder)
    {
        builder.AddCheck<RsGeSoapHealthCheck>(
            "rsge-soap",
            failureStatus: HealthStatus.Degraded,
            tags: ["rsge", "external"]);

        builder.AddCheck<RabbitMqQueueDepthHealthCheck>(
            "rsge-queue-depth",
            failureStatus: HealthStatus.Degraded,
            tags: ["rabbitmq", "queue"]);

        return builder;
    }
}
