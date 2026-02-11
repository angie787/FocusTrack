using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace FocusTrack.Session.Infrastructure.Messaging;

public class RabbitMQOutboxPublisher : IOutboxMessagePublisher, IAsyncDisposable
{
    private const string ExchangeName = "session-events";
    private readonly string _hostName;
    private readonly ILogger<RabbitMQOutboxPublisher> _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public RabbitMQOutboxPublisher(IConfiguration configuration, ILogger<RabbitMQOutboxPublisher> logger)
    {
        _hostName = configuration["RabbitMQ:HostName"] ?? "rabbitmq";
        _logger = logger;
    }

    private async Task EnsureConnectedAsync(CancellationToken ct)
    {
        if (_channel != null && _channel.IsOpen)
            return;

        await _lock.WaitAsync(ct);
        try
        {
            if (_channel != null && _channel.IsOpen)
                return;

            _connection?.Dispose();
            _channel?.Dispose();

            var factory = new ConnectionFactory
            {
                HostName = _hostName,
                VirtualHost = "/"
            };
            _connection = await factory.CreateConnectionAsync(ct);
            var channelOptions = new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true);
            _channel = await _connection.CreateChannelAsync(channelOptions, ct);
            await _channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true, cancellationToken: ct);
            _logger.LogInformation("RabbitMQ outbox publisher connected to {HostName}, exchange {Exchange}", _hostName, ExchangeName);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task PublishAsync(string routingKey, string payload, CancellationToken ct = default)
    {
        await EnsureConnectedAsync(ct);

        var body = Encoding.UTF8.GetBytes(payload);
        await _channel!.BasicPublishAsync(ExchangeName, routingKey, body, cancellationToken: ct);

        _logger.LogInformation("Published event {RoutingKey} to exchange {Exchange}", routingKey, ExchangeName);
    }

    public async ValueTask DisposeAsync()
    {
        await _lock.WaitAsync();
        try
        {
            _channel?.Dispose();
            _connection?.Dispose();
            _channel = null;
            _connection = null;
        }
        finally
        {
            _lock.Release();
        }
    }
}
