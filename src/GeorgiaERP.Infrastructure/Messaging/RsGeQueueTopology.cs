namespace GeorgiaERP.Infrastructure.Messaging;

/// <summary>
/// Names of the RabbitMQ exchanges and queues that make up the RS.GE submission
/// pipeline. A primary work queue is bound to a direct exchange; messages that
/// exhaust their retries are routed to a dead-letter queue for manual review.
/// A priority queue allows urgent compliance submissions to be processed first.
/// </summary>
public static class RsGeQueueTopology
{
    public const string Exchange = "rsge.exchange";
    public const string SubmissionQueue = "rsge.submissions";
    public const string SubmissionRoutingKey = "rsge.submit";

    public const string DeadLetterExchange = "rsge.dlx";
    public const string DeadLetterQueue = "rsge.deadletter";
    public const string DeadLetterRoutingKey = "rsge.dead";

    /// <summary>Maximum message TTL (milliseconds) in the submission queue. Messages older
    /// than 24 hours are dead-lettered since they likely represent stale state.</summary>
    public const int MessageTtlMs = 86_400_000; // 24 hours

    /// <summary>Maximum priority level supported by the submission queue (0-10).
    /// Higher priority is used for documents approaching their RS.GE deadline.</summary>
    public const int MaxPriority = 10;

    /// <summary>Priority for normal submissions.</summary>
    public const int NormalPriority = 0;

    /// <summary>Priority for documents within 5 days of their RS.GE deadline.</summary>
    public const int UrgentPriority = 5;

    /// <summary>Priority for documents within 1 day of their RS.GE deadline.</summary>
    public const int CriticalPriority = 10;
}
