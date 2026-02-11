using FocusTrack.Session.Application.Common;
using FocusTrack.Session.Application.Interfaces;
using FocusTrack.Session.Domain.Events;
using MediatR;

namespace FocusTrack.Session.Infrastructure.Handlers;

//In-process event handler: writes domain events to the outbox in the same transaction as the command.
//Implemented as concrete handlers per event type so MediatR/DI register correctly (open generic caused conversion errors)
public sealed class DomainEventNotificationHandlers : INotificationHandler<DomainEventNotification<SessionCreatedEvent>>,
    INotificationHandler<DomainEventNotification<SessionUpdatedEvent>>,
    INotificationHandler<DomainEventNotification<SessionDeletedEvent>>,
    INotificationHandler<DomainEventNotification<SessionSharedEvent>>,
    INotificationHandler<DomainEventNotification<UserStatusChangedEvent>>
{
    private readonly IDomainEventPublisher _publisher;

    public DomainEventNotificationHandlers(IDomainEventPublisher publisher)
    {
        _publisher = publisher;
    }

    public Task Handle(DomainEventNotification<SessionCreatedEvent> notification, CancellationToken cancellationToken)
        => _publisher.PublishAsync(notification.DomainEvent);

    public Task Handle(DomainEventNotification<SessionUpdatedEvent> notification, CancellationToken cancellationToken)
        => _publisher.PublishAsync(notification.DomainEvent);

    public Task Handle(DomainEventNotification<SessionDeletedEvent> notification, CancellationToken cancellationToken)
        => _publisher.PublishAsync(notification.DomainEvent);

    public Task Handle(DomainEventNotification<SessionSharedEvent> notification, CancellationToken cancellationToken)
        => _publisher.PublishAsync(notification.DomainEvent);

    public Task Handle(DomainEventNotification<UserStatusChangedEvent> notification, CancellationToken cancellationToken)
        => _publisher.PublishAsync(notification.DomainEvent);
}
