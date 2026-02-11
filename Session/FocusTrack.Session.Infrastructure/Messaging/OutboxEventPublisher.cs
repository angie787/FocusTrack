using System.Text.Json;
using FocusTrack.Session.Application.Interfaces;
using FocusTrack.Session.Infrastructure.Persistence;

namespace FocusTrack.Session.Infrastructure.Messaging;

//Writes domain events to the outbox table in the same transaction as the session change.
//Does not call SaveChanges; the controller (or caller) must call SaveChanges after
public class OutboxEventPublisher : IDomainEventPublisher
{
    private readonly SessionDbContext _context;

    public OutboxEventPublisher(SessionDbContext context)
    {
        _context = context;
    }

    public Task PublishAsync<T>(T domainEvent) where T : class
    {
        var eventType = typeof(T).Name;
        var payload = JsonSerializer.Serialize(domainEvent);
        _context.DomainEventOutbox.Add(new DomainEventOutbox
        {
            EventType = eventType,
            Payload = payload,
            CreatedAt = DateTimeOffset.UtcNow
        });
        return Task.CompletedTask;
    }
}
