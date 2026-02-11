using FocusTrack.Session.Application.Commands;
using FocusTrack.Session.Application.Common;
using FocusTrack.Session.Domain.Events;
using FocusTrack.Session.Domain.Models;
using FocusTrack.Session.Application.Interfaces;
using FocusTrack.Session.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FocusTrack.Session.Infrastructure.Handlers;

public sealed class CreateSessionCommandHandler : IRequestHandler<CreateSessionCommand, CreateSessionResult>
{
    private readonly SessionDbContext _context;
    private readonly IMediator _mediator;
    private readonly IMonthlyFocusProjection _monthlyProjection;

    public CreateSessionCommandHandler(SessionDbContext context, IMediator mediator, IMonthlyFocusProjection monthlyProjection)
    {
        _context = context;
        _mediator = mediator;
        _monthlyProjection = monthlyProjection;
    }

    public async Task<CreateSessionResult> Handle(CreateSessionCommand request, CancellationToken cancellationToken)
    {
        var r = request.Request;
        var session = new Domain.Models.Session
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Topic = r.Topic,
            StartTime = r.StartTime,
            Mode = r.Mode,
            DurationMin = Domain.Models.Session.ComputeDurationMin(r.StartTime, null),
            IsDailyGoalAchieved = false
        };

        _context.Sessions.Add(session);
        await _mediator.Publish(new DomainEventNotification<SessionCreatedEvent>(
            new SessionCreatedEvent(session.Id, session.UserId, session.Topic, session.StartTime)), cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await _monthlyProjection.RecomputeForUserAndMonthAsync(session.UserId, session.StartTime.Year, session.StartTime.Month, cancellationToken);

        return new CreateSessionResult(session);
    }
}
