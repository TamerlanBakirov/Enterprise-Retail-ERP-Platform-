namespace GeorgiaERP.Application.Webhooks.DTOs;

public record WebhookSubscriptionDto(
    Guid Id,
    string Name,
    string Url,
    IReadOnlyList<string> EventTypes,
    bool IsActive,
    int MaxRetries,
    int ConsecutiveFailures,
    DateTimeOffset? LastDeliveryAt,
    string? LastDeliveryStatus,
    DateTimeOffset CreatedAt);

public record CreateWebhookRequest(
    string Name,
    string Url,
    string Secret,
    List<string> EventTypes,
    int MaxRetries = 3);

public record UpdateWebhookRequest(
    string? Name,
    string? Url,
    List<string>? EventTypes,
    int? MaxRetries);

public record WebhookDeliveryLogDto(
    Guid Id,
    Guid SubscriptionId,
    string EventType,
    int AttemptNumber,
    bool Success,
    int? HttpStatusCode,
    string? ErrorMessage,
    DateTimeOffset AttemptedAt,
    int DurationMs);
