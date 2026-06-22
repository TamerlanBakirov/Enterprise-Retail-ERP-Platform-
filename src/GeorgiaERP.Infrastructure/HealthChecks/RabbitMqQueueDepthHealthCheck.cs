using GeorgiaERP.Infrastructure.Messaging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace GeorgiaERP.Infrastructure.HealthChecks;

/// <summary>
/// Health check that monitors RabbitMQ queue depths for the RS.GE submission and
/// dead-letter queues. Reports degraded when the submission queue exceeds a
/// configurable threshold, and unhealthy when the dead-letter queue has messages
/// (indicating failed submissions that need manual review).
/// </summary>
public sealed class RabbitMqQueueDepthHealthCheck : IHealthCheck
{
    private readonly IRabbitMqConnection _connection;
    private readonly ILogger<RabbitMqQueueDepthHealthCheck> _logger;

    /// <summary>Submission queue depth above this is considered degraded.</summary>
    private const uint SubmissionQueueWarningThreshold = 100;

    /// <summary>Dead-letter queue depth above this is considered unhealthy.</summary>
    private const uint DeadLetterQueueWarningThreshold = 0;

    public RabbitMqQueueDepthHealthCheck(
        IRabbitMqConnection connection,
        ILogger<RabbitMqQueueDepthHealthCheck> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = await _connection.GetConnectionAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

            // QueueDeclarePassiveAsync returns the queue info without modifying it.
            var submissionInfo = await channel.QueueDeclarePassiveAsync(
                RsGeQueueTopology.SubmissionQueue, cancellationToken);
            var deadLetterInfo = await channel.QueueDeclarePassiveAsync(
                RsGeQueueTopology.DeadLetterQueue, cancellationToken);

            var data = new Dictionary<string, object>
            {
                ["submission_queue_depth"] = submissionInfo.MessageCount,
                ["submission_queue_consumers"] = submissionInfo.ConsumerCount,
                ["deadletter_queue_depth"] = deadLetterInfo.MessageCount
            };

            if (deadLetterInfo.MessageCount > DeadLetterQueueWarningThreshold)
            {
                _logger.LogWarning(
                    "RS.GE dead-letter queue has {Count} message(s) requiring manual review",
                    deadLetterInfo.MessageCount);

                return HealthCheckResult.Unhealthy(
                    $"RS.GE dead-letter queue has {deadLetterInfo.MessageCount} unresolved message(s)",
                    data: data);
            }

            if (submissionInfo.MessageCount > SubmissionQueueWarningThreshold)
            {
                return HealthCheckResult.Degraded(
                    $"RS.GE submission queue depth ({submissionInfo.MessageCount}) exceeds threshold ({SubmissionQueueWarningThreshold})",
                    data: data);
            }

            if (submissionInfo.ConsumerCount == 0)
            {
                return HealthCheckResult.Degraded(
                    "RS.GE submission queue has no active consumers",
                    data: data);
            }

            return HealthCheckResult.Healthy(
                $"RabbitMQ queues healthy (submissions: {submissionInfo.MessageCount}, DLQ: {deadLetterInfo.MessageCount})",
                data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ queue depth health check failed");
            return HealthCheckResult.Unhealthy(
                "Cannot connect to RabbitMQ to check queue depths",
                exception: ex);
        }
    }
}
