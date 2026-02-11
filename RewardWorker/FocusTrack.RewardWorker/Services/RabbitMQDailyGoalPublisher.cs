using System.Text;
using System.Text.Json;
using FocusTrack.RewardWorker.Events;
using RabbitMQ.Client;

namespace FocusTrack.RewardWorker.Services;

public class RabbitMQDailyGoalPublisher : IDailyGoalEventPublisher
{
    private readonly string _hostName;
    private readonly ILogger<RabbitMQDailyGoalPublisher> _logger;

    public RabbitMQDailyGoalPublisher(IConfiguration configuration, ILogger<RabbitMQDailyGoalPublisher> logger)
    {
        _hostName = configuration["RabbitMQ:HostName"] ?? "localhost";
        _logger = logger;
    }

    public async Task PublishAsync(DailyGoalAchievedEvent e, CancellationToken ct = default)
    {
        try
        {
            var factory = new ConnectionFactory { HostName = _hostName };
            await using var connection = await factory.CreateConnectionAsync(ct);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

            var exchangeName = "session-events";
            await channel.ExchangeDeclareAsync(exchange: exchangeName, type: ExchangeType.Topic, cancellationToken: ct);

            var json = JsonSerializer.Serialize(e);
            var body = Encoding.UTF8.GetBytes(json);
            var routingKey = nameof(DailyGoalAchievedEvent);

            await channel.BasicPublishAsync(exchangeName, routingKey, body, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish DailyGoalAchievedEvent for session {SessionId}", e.SessionId);
        }
    }
}
