using System.Text;
using System.Text.Json;
using FocusTrack.Session.Application.Interfaces;
using RabbitMQ.Client;

namespace FocusTrack.Session.Infrastructure.Messaging;

public class RabbitMQPublisher : IDomainEventPublisher
{
    private const string HostName = "rabbitmq";

    public async Task PublishAsync<T>(T domainEvent) where T : class
    {
        var factory = new ConnectionFactory { HostName = HostName };

        //Creating connection and channel
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        //Use a Topic exchange for flexible routing
        var exchangeName = "session-events";
        await channel.ExchangeDeclareAsync(exchange: exchangeName, type: ExchangeType.Topic);

        var json = JsonSerializer.Serialize(domainEvent);
        var body = Encoding.UTF8.GetBytes(json);

        //Routing key is the event name
        var routingKey = typeof(T).Name;

        await channel.BasicPublishAsync(
            exchange: exchangeName,
            routingKey: routingKey,
            body: body);
    }
}