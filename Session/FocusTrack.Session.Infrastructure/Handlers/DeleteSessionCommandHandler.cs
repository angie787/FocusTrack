using FocusTrack.Session.Application.Commands;
using FocusTrack.Session.Application.Common;
using FocusTrack.Session.Application.Interfaces;
using FocusTrack.Session.Domain.Events;
using FocusTrack.Session.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FocusTrack.Session.Infrastructure.Handlers;

public sealed class DeleteSessionCommandHandler : IRequestHandler<DeleteSessionCommand, bool>
{
    private readonly SessionDbContext _context;
    private readonly IMediator _mediator;
    private readonly IMonthlyFocusProjection _monthlyProjection;

    public DeleteSessionCommandHandler(SessionDbContext context, IMediator mediator, IMonthlyFocusProjection monthlyProjection)
    {
        _context = context;
        _mediator = mediator;
        _monthlyProjection = monthlyProjection;
    }

    public async Task<bool> Handle(DeleteSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _context.Sessions.FindAsync(new object[] { request.SessionId }, cancellationToken);
        if (session == null || session.UserId != request.UserId) return false;

        var userId = session.UserId;
        var year = session.StartTime.Year;
        var month = session.StartTime.Month;

        _context.Sessions.Remove(session);
        await _mediator.Publish(new DomainEventNotification<SessionDeletedEvent>(
            new SessionDeletedEvent(request.SessionId, request.UserId)), cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await _monthlyProjection.RecomputeForUserAndMonthAsync(userId, year, month, cancellationToken);

        return true;
    }
}
