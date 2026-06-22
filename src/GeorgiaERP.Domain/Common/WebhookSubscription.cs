namespace GeorgiaERP.Domain.Common;

/// <summary>
/// Represents a webhook subscription that receives event notifications via HTTP POST.
/// Supports HMAC-SHA256 signature verification for payload integrity.
/// </summary>
public class WebhookSubscription : AuditableEntity
{
    public string Name { get; private set; } = default!;
    public string Url { get; private set; } = default!;
    public string Secret { get; private set; } = default!;
    public string EventTypes { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public int MaxRetries { get; private set; }
    public int ConsecutiveFailures { get; private set; }
    public DateTimeOffset? LastDeliveryAt { get; private set; }
    public string? LastDeliveryStatus { get; private set; }

    private WebhookSubscription() { }

    public static WebhookSubscription Create(
        string name,
        string url,
        string secret,
        IEnumerable<string> eventTypes,
        int maxRetries = 3)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Webhook name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Webhook URL is required.", nameof(url));
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "https" && uri.Scheme != "http"))
            throw new ArgumentException("Webhook URL must be a valid HTTP(S) URL.", nameof(url));
        if (string.IsNullOrWhiteSpace(secret))
            throw new ArgumentException("Webhook secret is required for HMAC verification.", nameof(secret));

        var eventTypeList = eventTypes.ToList();
        if (eventTypeList.Count == 0)
            throw new ArgumentException("At least one event type is required.", nameof(eventTypes));

        return new WebhookSubscription
        {
            Name = name,
            Url = url,
            Secret = secret,
            EventTypes = string.Join(",", eventTypeList),
            IsActive = true,
            MaxRetries = maxRetries,
            ConsecutiveFailures = 0
        };
    }

    public bool SubscribesTo(string eventType)
    {
        var types = EventTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return types.Contains("*") || types.Contains(eventType, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<string> GetEventTypes()
    {
        return EventTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public void RecordDelivery(bool success, string? status = null)
    {
        LastDeliveryAt = DateTimeOffset.UtcNow;
        LastDeliveryStatus = status;

        if (success)
        {
            ConsecutiveFailures = 0;
        }
        else
        {
            ConsecutiveFailures++;
            // Auto-disable after too many consecutive failures
            if (ConsecutiveFailures >= MaxRetries * 3)
                IsActive = false;
        }
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public void Update(string? name = null, string? url = null, IEnumerable<string>? eventTypes = null, int? maxRetries = null)
    {
        if (name is not null) Name = name;
        if (url is not null)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "https" && uri.Scheme != "http"))
                throw new ArgumentException("Webhook URL must be a valid HTTP(S) URL.", nameof(url));
            Url = url;
        }
        if (eventTypes is not null)
        {
            var list = eventTypes.ToList();
            if (list.Count == 0)
                throw new ArgumentException("At least one event type is required.", nameof(eventTypes));
            EventTypes = string.Join(",", list);
        }
        if (maxRetries.HasValue) MaxRetries = maxRetries.Value;
    }
}
