using FocusTrack.Session.Application.Commands;
using FocusTrack.Session.Application.Common;
using FocusTrack.Session.Domain.Events;
using FocusTrack.Session.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FocusTrack.Session.Infrastructure.Handlers;

public sealed class ShareSessionCommandHandler : IRequestHandler<ShareSessionCommand, bool>
{
    private readonly SessionDbContext _context;
    private readonly IMediator _mediator;

    public ShareSessionCommandHandler(SessionDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    public async Task<bool> Handle(ShareSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _context.Sessions.FindAsync(new object[] { request.SessionId }, cancellationToken);
        if (session == null || session.UserId != request.UserId) return false;

        if (request.Request.UserIds == null || request.Request.UserIds.Count == 0) return false;

        var now = DateTimeOffset.UtcNow;
        foreach (var recipientId in request.Request.UserIds.Distinct())
        {
            if (string.IsNullOrEmpty(recipientId)) continue;
            if (await _context.SessionShares.AnyAsync(ss => ss.SessionId == request.SessionId && ss.SharedWithUserId == recipientId, cancellationToken))
                continue;
            _context.SessionShares.Add(new SessionShare
            {
                SessionId = request.SessionId,
                SharedWithUserId = recipientId,
                SharedByUserId = request.UserId,
                SharedAt = now
            });
        }

        _context.SessionShareAudits.Add(new SessionShareAudit
        {
            SessionId = request.SessionId,
            PerformedByUserId = request.UserId,
            Action = "SharedWithUsers",
            Details = string.Join(",", request.Request.UserIds),
            CreatedAt = now
        });

        await _mediator.Publish(new DomainEventNotification<SessionSharedEvent>(
            new SessionSharedEvent(request.SessionId, request.UserId, request.Request.UserIds.ToList(), now)), cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
