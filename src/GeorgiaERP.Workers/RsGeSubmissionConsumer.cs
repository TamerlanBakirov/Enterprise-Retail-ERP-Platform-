using System.Text;
using System.Text.Json;
using GeorgiaERP.Application.Compliance;
using GeorgiaERP.Infrastructure.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GeorgiaERP.Workers;

/// <summary>
/// Consumes the RS.GE submission queue and drives each message through the
/// submission processor. Acknowledgement strategy:
/// <list type="bullet">
/// <item>Success — ack and remove from the queue.</item>
/// <item>Permanent failure (RS.GE rejection) — dead-letter for manual review.</item>
/// <item>Transient failure — re-publish with exponential backoff until the retry
/// budget is exhausted, then dead-letter.</item>
/// </list>
/// Prefetch is 1 so the compliance stream is processed in order and a slow
/// submission never starves acknowledgement of others.
/// </summary>
public class RsGeSubmissionConsumer : BackgroundService
{
    private readonly IRabbitMqConnection _connection;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RsGeQueueOptions _options;
    private readonly ILogger<RsGeSubmissionConsumer> _logger;

    private IChannel? _channel;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public RsGeSubmissionConsumer(
        IRabbitMqConnection connection,
        IServiceScopeFactory scopeFactory,
        IOptions<RsGeQueueOptions> options,
        ILogger<RsGeSubmissionConsumer> logger)
    {
        _connection = connection;
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // The broker may not be up yet at startup; keep retrying the initial
        // connection so the worker self-heals once RabbitMQ becomes available.
        while (!stoppingToken.IsCancellationRequested && _channel is null)
        {
            try
            {
                await _connection.EnsureTopologyAsync(stoppingToken);
                var connection = await _connection.GetConnectionAsync(stoppingToken);
                _channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += OnMessageReceivedAsync;

                await _channel.BasicConsumeAsync(
                    queue: RsGeQueueTopology.SubmissionQueue,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken);

                _logger.LogInformation("RS.GE submission consumer started, listening on {Queue}", RsGeQueueTopology.SubmissionQueue);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Failed to start RS.GE consumer; retrying in 10s");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        // Keep the background service alive; message handling is event-driven.
        await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { }, TaskScheduler.Default);
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        if (_channel is null)
            return;

        RsGeSubmissionMessage? message = null;
        try
        {
            message = JsonSerializer.Deserialize<RsGeSubmissionMessage>(ea.Body.Span, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Malformed RS.GE message; dead-lettering");
            await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        if (message is null)
        {
            await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IRsGeSubmissionProcessor>();
            var result = await processor.ProcessAsync(message);

            switch (result.Outcome)
            {
                case RsGeSubmissionOutcome.Succeeded:
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    break;

                case RsGeSubmissionOutcome.PermanentFailure:
                    _logger.LogError("Permanent failure for document {DocumentId}; dead-lettering: {Detail}",
                        message.FiscalDocumentId, result.Detail);
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                    break;

                case RsGeSubmissionOutcome.TransientFailure:
                    await HandleTransientAsync(message, ea);
                    break;
            }
        }
        catch (Exception ex)
        {
            // Unexpected processing error — treat as transient so the message is retried.
            _logger.LogError(ex, "Unhandled error processing document {DocumentId}", message.FiscalDocumentId);
            await HandleTransientAsync(message, ea);
        }
    }

    private async Task HandleTransientAsync(RsGeSubmissionMessage message, BasicDeliverEventArgs ea)
    {
        if (_channel is null)
            return;

        if (message.Attempt >= _options.MaxRetryCount)
        {
            _logger.LogError("Document {DocumentId} exhausted {Max} retries; dead-lettering",
                message.FiscalDocumentId, _options.MaxRetryCount);
            await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        var delay = ComputeBackoff(message.Attempt);
        _logger.LogWarning("Transient failure for document {DocumentId}; retrying attempt {Next} in {Delay}s",
            message.FiscalDocumentId, message.Attempt + 1, delay.TotalSeconds);

        // Hold the delivery unacked during the backoff (prefetch=1 serializes the
        // stream), then re-publish the next attempt and ack the original. If the
        // process dies mid-delay the unacked message is redelivered on restart.
        await Task.Delay(delay);

        using var scope = _scopeFactory.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IRsGeQueuePublisher>();
        await publisher.PublishAsync(message.NextAttempt());

        await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
    }

    private TimeSpan ComputeBackoff(int attempt)
    {
        // Exponential backoff: base * 2^(attempt-1), capped at the configured max.
        var seconds = _options.RetryBaseDelaySeconds * Math.Pow(2, Math.Max(0, attempt - 1));
        seconds = Math.Min(seconds, _options.RetryMaxDelaySeconds);
        return TimeSpan.FromSeconds(seconds);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
            await _channel.DisposeAsync();
        }
        await base.StopAsync(cancellationToken);
    }
}
