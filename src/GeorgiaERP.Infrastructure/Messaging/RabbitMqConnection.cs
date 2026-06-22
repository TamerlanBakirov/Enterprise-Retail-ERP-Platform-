using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace GeorgiaERP.Infrastructure.Messaging;

/// <summary>
/// Owns the single long-lived RabbitMQ connection for the process and declares
/// the RS.GE topology (exchanges, work queue, dead-letter queue) on first use.
/// Channels are cheap and created per-operation by callers; the connection is not.
/// </summary>
public interface IRabbitMqConnection : IAsyncDisposable
{
    Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken = default);
    Task EnsureTopologyAsync(CancellationToken cancellationToken = default);
}

public sealed class RabbitMqConnection : IRabbitMqConnection
{
    private readonly RsGeQueueOptions _options;
    private readonly ILogger<RabbitMqConnection> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private IConnection? _connection;
    private bool _topologyDeclared;

    public RabbitMqConnection(IOptions<RsGeQueueOptions> options, ILogger<RabbitMqConnection> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (_connection is { IsOpen: true })
            return _connection;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await ConnectLockedAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task EnsureTopologyAsync(CancellationToken cancellationToken = default)
    {
        if (_topologyDeclared)
            return;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_topologyDeclared)
                return;

            var connection = await ConnectLockedAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

            // Dead-letter side: messages that exhaust retries land here for manual review.
            await channel.ExchangeDeclareAsync(
                RsGeQueueTopology.DeadLetterExchange, ExchangeType.Direct, durable: true, autoDelete: false,
                cancellationToken: cancellationToken);
            await channel.QueueDeclareAsync(
                RsGeQueueTopology.DeadLetterQueue, durable: true, exclusive: false, autoDelete: false,
                cancellationToken: cancellationToken);
            await channel.QueueBindAsync(
                RsGeQueueTopology.DeadLetterQueue, RsGeQueueTopology.DeadLetterExchange,
                RsGeQueueTopology.DeadLetterRoutingKey, cancellationToken: cancellationToken);

            // Primary work exchange + queue, configured to dead-letter on rejection.
            await channel.ExchangeDeclareAsync(
                RsGeQueueTopology.Exchange, ExchangeType.Direct, durable: true, autoDelete: false,
                cancellationToken: cancellationToken);

            var args = new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = RsGeQueueTopology.DeadLetterExchange,
                ["x-dead-letter-routing-key"] = RsGeQueueTopology.DeadLetterRoutingKey,
                ["x-message-ttl"] = RsGeQueueTopology.MessageTtlMs,
                ["x-max-priority"] = RsGeQueueTopology.MaxPriority
            };

            await channel.QueueDeclareAsync(
                RsGeQueueTopology.SubmissionQueue, durable: true, exclusive: false, autoDelete: false,
                arguments: args, cancellationToken: cancellationToken);
            await channel.QueueBindAsync(
                RsGeQueueTopology.SubmissionQueue, RsGeQueueTopology.Exchange,
                RsGeQueueTopology.SubmissionRoutingKey, cancellationToken: cancellationToken);

            _topologyDeclared = true;
            _logger.LogInformation("RS.GE queue topology declared");
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Returns an open connection, creating one if needed. Caller must hold <see cref="_gate"/>.</summary>
    private async Task<IConnection> ConnectLockedAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true })
            return _connection;

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true
        };

        _connection = await factory.CreateConnectionAsync("georgia-erp", cancellationToken);
        _logger.LogInformation("Connected to RabbitMQ at {Host}:{Port}", _options.HostName, _options.Port);
        return _connection;
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }
        _gate.Dispose();
    }
}
