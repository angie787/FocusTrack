namespace FocusTrack.Session.Application.Common;

//In-process MediatR notification that wraps a domain event. Handlers write to the outbox in the same transaction.
public sealed class DomainEventNotification<T> : MediatR.INotification where T : class
{
    public T DomainEvent { get; }

    public DomainEventNotification(T domainEvent)
    {
        DomainEvent = domainEvent ?? throw new ArgumentNullException(nameof(domainEvent));
    }
}
