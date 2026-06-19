namespace GeorgiaERP.Infrastructure.Messaging;

/// <summary>
/// Names of the RabbitMQ exchanges and queues that make up the RS.GE submission
/// pipeline. A primary work queue is bound to a direct exchange; messages that
/// exhaust their retries are routed to a dead-letter queue for manual review.
/// </summary>
public static class RsGeQueueTopology
{
    public const string Exchange = "rsge.exchange";
    public const string SubmissionQueue = "rsge.submissions";
    public const string SubmissionRoutingKey = "rsge.submit";

    public const string DeadLetterExchange = "rsge.dlx";
    public const string DeadLetterQueue = "rsge.deadletter";
    public const string DeadLetterRoutingKey = "rsge.dead";
}
