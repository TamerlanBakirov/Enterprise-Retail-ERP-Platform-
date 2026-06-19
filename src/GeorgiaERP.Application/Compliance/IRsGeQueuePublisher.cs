namespace GeorgiaERP.Application.Compliance;

/// <summary>
/// Publishes fiscal-document submission work onto the durable RS.GE queue.
/// Business operations call this after persisting their state so that RS.GE
/// communication happens asynchronously and never blocks the transaction.
/// </summary>
public interface IRsGeQueuePublisher
{
    Task PublishAsync(RsGeSubmissionMessage message, CancellationToken cancellationToken = default);
}
