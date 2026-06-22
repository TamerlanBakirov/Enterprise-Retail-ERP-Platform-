namespace GeorgiaERP.Application.Common;

/// <summary>
/// Service for delivering webhook event notifications to subscribed endpoints.
/// </summary>
public interface IWebhookService
{
    /// <summary>
    /// Delivers an event payload to all active webhook subscriptions that match the event type.
    /// Handles retries and HMAC-SHA256 signature generation.
    /// </summary>
    Task DeliverAsync(string eventType, object payload, CancellationToken cancellationToken = default);
}
