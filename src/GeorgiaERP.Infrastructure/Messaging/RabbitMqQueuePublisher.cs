using System.Diagnostics;
using System.Text;
using System.Text.Json;
using GeorgiaERP.Application.Compliance;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace GeorgiaERP.Infrastructure.Messaging;

/// <summary>
/// Publishes <see cref="RsGeSubmissionMessage"/>s onto the durable work queue.
/// Messages are persistent so they survive a broker restart; the fiscal document
/// itself is the source of truth, so a lost publish is recoverable by re-queueing.
/// </summary>
public class RabbitMqQueuePublisher : IRsGeQueuePublisher
{
    private readonly IRabbitMqConnection _connection;
    private readonly ILogger<RabbitMqQueuePublisher> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public RabbitMqQueuePublisher(IRabbitMqConnection connection, ILogger<RabbitMqQueuePublisher> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task PublishAsync(RsGeSubmissionMessage message, CancellationToken cancellationToken = default)
    {
        await _connection.EnsureTopologyAsync(cancellationToken);
        var connection = await _connection.GetConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        var body = JsonSerializer.SerializeToUtf8Bytes(message, JsonOptions);

        // Use FiscalDocumentId + Operation + Attempt as a deterministic MessageId
        // for idempotent deduplication on the consumer side.
        var messageId = $"{message.FiscalDocumentId}:{message.Operation}:{message.Attempt}";
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = messageId,
            CorrelationId = correlationId,
            Type = message.Operation.ToString(),
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        await channel.BasicPublishAsync(
            exchange: RsGeQueueTopology.Exchange,
            routingKey: RsGeQueueTopology.SubmissionRoutingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Published RS.GE submission for document {DocumentId} (operation {Operation}, attempt {Attempt})",
            message.FiscalDocumentId, message.Operation, message.Attempt);
    }
}
