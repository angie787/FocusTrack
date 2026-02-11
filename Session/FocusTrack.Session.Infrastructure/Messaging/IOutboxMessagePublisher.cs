namespace FocusTrack.Session.Infrastructure.Messaging;

//publishes a raw message to the broker (used by OutboxProcessor to send stored events)
public interface IOutboxMessagePublisher
{
    Task PublishAsync(string routingKey, string payload, CancellationToken ct = default);
}
