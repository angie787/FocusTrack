using FocusTrack.Session.Application.Commands;
using FocusTrack.Session.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FocusTrack.Session.Infrastructure.Handlers;

public sealed class UnshareSessionCommandHandler : IRequestHandler<UnshareSessionCommand, bool>
{
    private readonly SessionDbContext _context;

    public UnshareSessionCommandHandler(SessionDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UnshareSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _context.Sessions.FindAsync(new object[] { request.SessionId }, cancellationToken);
        if (session == null || session.UserId != request.UserId) return false;

        var share = await _context.SessionShares
            .FirstOrDefaultAsync(ss => ss.SessionId == request.SessionId && ss.SharedWithUserId == request.SharedWithUserId, cancellationToken);
        if (share == null) return false;

        _context.SessionShares.Remove(share);
        _context.SessionShareAudits.Add(new SessionShareAudit
        {
            SessionId = request.SessionId,
            PerformedByUserId = request.UserId,
            Action = "Unshared",
            Details = request.SharedWithUserId,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
