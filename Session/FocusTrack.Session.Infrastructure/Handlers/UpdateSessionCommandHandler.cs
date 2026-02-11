using FocusTrack.Session.Application.Commands;
using FocusTrack.Session.Application.Interfaces;
using FocusTrack.Session.Domain.Events;
using FocusTrack.Session.Domain.Models;
using FocusTrack.Session.Application.Common;
using FocusTrack.Session.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FocusTrack.Session.Infrastructure.Handlers;

public sealed class UpdateSessionCommandHandler : IRequestHandler<UpdateSessionCommand, bool>
{
    private readonly SessionDbContext _context;
    private readonly IMediator _mediator;
    private readonly IMonthlyFocusProjection _monthlyProjection;

    public UpdateSessionCommandHandler(SessionDbContext context, IMediator mediator, IMonthlyFocusProjection monthlyProjection)
    {
        _context = context;
        _mediator = mediator;
        _monthlyProjection = monthlyProjection;
    }

    public async Task<bool> Handle(UpdateSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _context.Sessions.FindAsync(new object[] { request.SessionId }, cancellationToken);
        if (session == null || session.UserId != request.UserId) return false;

        var oldYear = session.StartTime.Year;
        var oldMonth = session.StartTime.Month;
        var r = request.Request;

        session.Topic = r.Topic;
        session.EndTime = r.EndTime;
        session.Mode = r.Mode;
        session.DurationMin = Domain.Models.Session.ComputeDurationMin(session.StartTime, r.EndTime);

        await _mediator.Publish(new DomainEventNotification<SessionUpdatedEvent>(
            new SessionUpdatedEvent(session.Id, session.UserId, session.Topic, session.EndTime, session.StartTime, session.DurationMin)), cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _monthlyProjection.RecomputeForUserAndMonthAsync(session.UserId, oldYear, oldMonth, cancellationToken);
        await _monthlyProjection.RecomputeForUserAndMonthAsync(session.UserId, session.StartTime.Year, session.StartTime.Month, cancellationToken);

        return true;
    }
}
