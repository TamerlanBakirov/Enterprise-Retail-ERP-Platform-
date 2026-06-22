using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GeorgiaERP.Infrastructure.Webhooks;

/// <summary>
/// Delivers webhook events to subscribed endpoints with HMAC-SHA256 signature verification.
/// Supports retry with exponential backoff and logs all delivery attempts.
/// </summary>
public class WebhookDeliveryService : IWebhookService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookDeliveryService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public WebhookDeliveryService(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookDeliveryService> logger)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task DeliverAsync(string eventType, object payload, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var subscriptions = await db.WebhookSubscriptions
            .AsNoTracking()
            .Where(w => w.IsActive)
            .ToListAsync(cancellationToken);

        var matching = subscriptions.Where(s => s.SubscribesTo(eventType)).ToList();
        if (matching.Count == 0)
        {
            _logger.LogDebug("No active webhook subscriptions for event {EventType}", eventType);
            return;
        }

        var payloadJson = JsonSerializer.Serialize(new
        {
            eventType,
            timestamp = DateTimeOffset.UtcNow,
            data = payload
        }, JsonOptions);

        foreach (var subscription in matching)
        {
            _ = DeliverToSubscriptionAsync(subscription, eventType, payloadJson, cancellationToken);
        }
    }

    private async Task DeliverToSubscriptionAsync(
        WebhookSubscription subscription,
        string eventType,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        var maxAttempts = Math.Max(1, subscription.MaxRetries + 1);

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            if (attempt > 1)
            {
                // Exponential backoff: 2^attempt seconds (2s, 4s, 8s, ...)
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                await Task.Delay(delay, cancellationToken);
            }

            try
            {
                var (success, statusCode, responseBody, durationMs) =
                    await SendWebhookAsync(subscription, payloadJson, cancellationToken);

                await LogDeliveryAsync(subscription.Id, eventType, payloadJson, attempt, success,
                    statusCode, responseBody, null, durationMs);

                await UpdateSubscriptionStatusAsync(subscription.Id, success,
                    success ? $"{statusCode} OK" : $"{statusCode} Failed");

                if (success)
                {
                    _logger.LogInformation(
                        "Webhook delivered: {EventType} -> {Url} (attempt {Attempt}, {StatusCode})",
                        eventType, subscription.Url, attempt, statusCode);
                    return;
                }

                _logger.LogWarning(
                    "Webhook delivery failed: {EventType} -> {Url} (attempt {Attempt}/{Max}, {StatusCode})",
                    eventType, subscription.Url, attempt, maxAttempts, statusCode);
            }
            catch (Exception ex)
            {
                await LogDeliveryAsync(subscription.Id, eventType, payloadJson, attempt, false,
                    null, null, ex.Message, 0);

                await UpdateSubscriptionStatusAsync(subscription.Id, false, $"Error: {ex.Message}");

                _logger.LogWarning(ex,
                    "Webhook delivery error: {EventType} -> {Url} (attempt {Attempt}/{Max})",
                    eventType, subscription.Url, attempt, maxAttempts);

                if (attempt >= maxAttempts)
                    return;
            }
        }
    }

    private async Task<(bool Success, int? StatusCode, string? ResponseBody, int DurationMs)>
        SendWebhookAsync(WebhookSubscription subscription, string payloadJson, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("webhook");
        client.Timeout = TimeSpan.FromSeconds(10);

        var signature = ComputeHmacSignature(payloadJson, subscription.Secret);

        var request = new HttpRequestMessage(HttpMethod.Post, subscription.Url)
        {
            Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Webhook-Signature", $"sha256={signature}");
        request.Headers.Add("X-Webhook-Id", Guid.NewGuid().ToString());
        request.Headers.Add("User-Agent", "GeorgiaERP-Webhook/1.0");

        var sw = Stopwatch.StartNew();
        var response = await client.SendAsync(request, cancellationToken);
        sw.Stop();

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var success = response.IsSuccessStatusCode;

        return (success, (int)response.StatusCode, responseBody, (int)sw.ElapsedMilliseconds);
    }

    public static string ComputeHmacSignature(string payload, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var hash = HMACSHA256.HashData(keyBytes, payloadBytes);
        return Convert.ToHexStringLower(hash);
    }

    private async Task LogDeliveryAsync(
        Guid subscriptionId, string eventType, string payload, int attempt,
        bool success, int? httpStatus, string? responseBody, string? errorMessage, int durationMs)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            var log = WebhookDeliveryLog.Create(
                subscriptionId, eventType, payload, attempt,
                success, httpStatus, responseBody, errorMessage, durationMs);

            db.WebhookDeliveryLogs.Add(log);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log webhook delivery attempt");
        }
    }

    private async Task UpdateSubscriptionStatusAsync(Guid subscriptionId, bool success, string? status)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            var subscription = await db.WebhookSubscriptions
                .FirstOrDefaultAsync(w => w.Id == subscriptionId);

            if (subscription is not null)
            {
                subscription.RecordDelivery(success, status);
                await db.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update webhook subscription status");
        }
    }
}
