using System.Text;
using System.Text.Json;
using FocusTrack.RewardWorker.Events;
using FocusTrack.RewardWorker.Services;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FocusTrack.RewardWorker;

public class SessionEventConsumer : BackgroundService
{
    private const string ExchangeName = "session-events";
    private const string QueueName = "reward-worker-session-events";
    private readonly string _hostName;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SessionEventConsumer> _logger;

    public SessionEventConsumer(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        ILogger<SessionEventConsumer> logger)
    {
        _hostName = configuration["RabbitMQ:HostName"] ?? "localhost";
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { HostName = _hostName, VirtualHost = "/" };
        IConnection? connection = null;
        IChannel? channel = null;

        try
        {
            connection = await factory.CreateConnectionAsync(stoppingToken);
            channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
            await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
            await channel.QueueBindAsync(QueueName, ExchangeName, "SessionCreatedEvent", null, false, stoppingToken);
            await channel.QueueBindAsync(QueueName, ExchangeName, "SessionUpdatedEvent", null, false, stoppingToken);
            await channel.QueueBindAsync(QueueName, ExchangeName, "SessionDeletedEvent", null, false, stoppingToken);
            await channel.BasicQosAsync(0, 1, false, stoppingToken);

            _logger.LogInformation("SessionEventConsumer connected to {HostName}, queue {Queue}", _hostName, QueueName);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    var routingKey = ea.RoutingKey;

                    await using (var scope = _scopeFactory.CreateAsyncScope())
                    {
                        if (routingKey == "SessionCreatedEvent")
                        {
                            var e = JsonSerializer.Deserialize<SessionCreatedEventDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (e != null)
                                _logger.LogInformation("Session created: SessionId={SessionId}, UserId={UserId}, Topic={Topic}", e.SessionId, e.UserId, e.Topic);
                        }
                        else
                        {
                            var dailyGoalService = scope.ServiceProvider.GetRequiredService<IDailyGoalService>();
                            if (routingKey == "SessionUpdatedEvent")
                            {
                                var e = JsonSerializer.Deserialize<SessionUpdatedEventDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                if (e != null)
                                    await dailyGoalService.OnSessionUpdatedAsync(e, stoppingToken);
                            }
                            else if (routingKey == "SessionDeletedEvent")
                            {
                                var e = JsonSerializer.Deserialize<SessionDeletedEventDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                if (e != null)
                                    await dailyGoalService.OnSessionDeletedAsync(e, stoppingToken);
                            }
                        }
                    }

                    await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message");
                    await channel.BasicNackAsync(ea.DeliveryTag, false, true, stoppingToken);
                }
            };

            await channel.BasicConsumeAsync(QueueName, false, "", false, false, null, consumer, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
                await Task.Delay(1000, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SessionEventConsumer failed");
        }
        finally
        {
            if (channel != null) await channel.CloseAsync();
            if (connection != null) await connection.CloseAsync();
        }
    }
}
