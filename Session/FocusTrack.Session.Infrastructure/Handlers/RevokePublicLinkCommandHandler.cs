using FocusTrack.Session.Application.Commands;
using FocusTrack.Session.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FocusTrack.Session.Infrastructure.Handlers;

public sealed class RevokePublicLinkCommandHandler : IRequestHandler<RevokePublicLinkCommand, bool>
{
    private readonly SessionDbContext _context;

    public RevokePublicLinkCommandHandler(SessionDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(RevokePublicLinkCommand request, CancellationToken cancellationToken)
    {
        var session = await _context.Sessions.FindAsync(new object[] { request.SessionId }, cancellationToken);
        if (session == null || session.UserId != request.UserId) return false;

        var link = await _context.SessionPublicLinks
            .Where(pl => pl.SessionId == request.SessionId && pl.RevokedAt == null)
            .OrderByDescending(pl => pl.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (link == null) return false;

        link.RevokedAt = DateTimeOffset.UtcNow;
        _context.SessionShareAudits.Add(new SessionShareAudit
        {
            SessionId = request.SessionId,
            PerformedByUserId = request.UserId,
            Action = "PublicLinkRevoked",
            Details = link.Token,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
