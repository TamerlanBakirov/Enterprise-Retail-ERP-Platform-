namespace GeorgiaERP.Infrastructure.Messaging;

/// <summary>
/// Tracks processed message IDs to ensure idempotent message handling.
/// Uses a sliding window of processed message IDs to detect and skip duplicates
/// that may occur during broker redelivery or recovery sweep re-enqueue.
/// </summary>
public interface IMessageDeduplicator
{
    /// <summary>
    /// Returns true if this message ID has already been processed.
    /// If not, marks it as processed atomically.
    /// </summary>
    Task<bool> IsDuplicateAsync(string messageId, CancellationToken cancellationToken = default);
}
