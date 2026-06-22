namespace GeorgiaERP.Domain.Common;

/// <summary>
/// Records each webhook delivery attempt for debugging and auditing.
/// </summary>
public class WebhookDeliveryLog : BaseEntity
{
    public Guid SubscriptionId { get; private set; }
    public string EventType { get; private set; } = default!;
    public string Payload { get; private set; } = default!;
    public int AttemptNumber { get; private set; }
    public int? HttpStatusCode { get; private set; }
    public string? ResponseBody { get; private set; }
    public string? ErrorMessage { get; private set; }
    public bool Success { get; private set; }
    public DateTimeOffset AttemptedAt { get; private set; }
    public int DurationMs { get; private set; }

    private WebhookDeliveryLog() { }

    public static WebhookDeliveryLog Create(
        Guid subscriptionId,
        string eventType,
        string payload,
        int attemptNumber,
        bool success,
        int? httpStatusCode = null,
        string? responseBody = null,
        string? errorMessage = null,
        int durationMs = 0)
    {
        return new WebhookDeliveryLog
        {
            SubscriptionId = subscriptionId,
            EventType = eventType,
            Payload = payload,
            AttemptNumber = attemptNumber,
            HttpStatusCode = httpStatusCode,
            ResponseBody = responseBody?.Length > 2000 ? responseBody[..2000] : responseBody,
            ErrorMessage = errorMessage?.Length > 2000 ? errorMessage[..2000] : errorMessage,
            Success = success,
            AttemptedAt = DateTimeOffset.UtcNow,
            DurationMs = durationMs
        };
    }
}
