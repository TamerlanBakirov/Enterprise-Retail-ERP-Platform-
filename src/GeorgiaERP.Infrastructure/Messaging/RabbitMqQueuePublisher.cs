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

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = Guid.NewGuid().ToString(),
            Type = message.Operation.ToString()
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
