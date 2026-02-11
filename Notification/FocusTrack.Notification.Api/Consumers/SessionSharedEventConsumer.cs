using System.Text;
using System.Text.Json;
using FocusTrack.Notification.Api.Hubs;
using FocusTrack.Notification.Api.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FocusTrack.Notification.Api.Consumers;

//Consumes SessionSharedEvent from Session service. Realtime via SignalR for online users, fallback email for offline
public class SessionSharedEventConsumer : BackgroundService
{
    private const string ExchangeName = "session-events";
    private const string QueueName = "notification-session-shared";
    private readonly string _hostName;
    private readonly IHubContext<NotificationsHub> _hubContext;
    private readonly IConnectionTracker _tracker;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SessionSharedEventConsumer> _logger;

    public SessionSharedEventConsumer(
        IConfiguration configuration,
        IHubContext<NotificationsHub> hubContext,
        IConnectionTracker tracker,
        IServiceScopeFactory scopeFactory,
        ILogger<SessionSharedEventConsumer> logger)
    {
        _hostName = configuration["RabbitMQ:HostName"] ?? "localhost";
        _hubContext = hubContext;
        _tracker = tracker;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { HostName = _hostName };
        IConnection? connection = null;
        IChannel? channel = null;

        try
        {
            connection = await factory.CreateConnectionAsync(stoppingToken);
            channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
            await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
            await channel.QueueBindAsync(QueueName, ExchangeName, "SessionSharedEvent", null, false, stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var e = JsonSerializer.Deserialize<SessionSharedEventDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (e != null)
                        await NotifyRecipientsAsync(e, stoppingToken);

                    await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing SessionSharedEvent");
                    await channel.BasicNackAsync(ea.DeliveryTag, false, true, stoppingToken);
                }
            };

            await channel.BasicConsumeAsync(QueueName, false, "", false, false, null, consumer, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
                await Task.Delay(1000, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SessionSharedEventConsumer failed");
        }
        finally
        {
            if (channel != null) await channel.CloseAsync();
            if (connection != null) await connection.CloseAsync();
        }
    }

    private async Task NotifyRecipientsAsync(SessionSharedEventDto e, CancellationToken ct)
    {
        var recipients = e.RecipientUserIds ?? new List<string>();
        foreach (var userId in recipients.Distinct())
        {
            if (string.IsNullOrEmpty(userId)) continue;

            if (_tracker.IsConnected(userId))
            {
                var payload = new { e.SessionId, e.OwnerUserId, e.SharedAt };
                await _hubContext.Clients.Group("user_" + userId).SendAsync("SessionShared", payload, ct);
                _logger.LogInformation("Session shared (SignalR): SessionId={SessionId}, Recipient={Recipient}", e.SessionId, userId);
            }
            else
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                await emailSender.SendSessionSharedNotificationAsync(userId, e.SessionId, e.OwnerUserId, ct);
            }
        }
    }
}
