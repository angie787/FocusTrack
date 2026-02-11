using FocusTrack.Session.Application.Queries;
using FocusTrack.Session.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FocusTrack.Session.Infrastructure.Handlers;

public sealed class GetByPublicLinkQueryHandler : IRequestHandler<GetByPublicLinkQuery, GetByPublicLinkResult?>
{
    private readonly SessionDbContext _context;

    public GetByPublicLinkQueryHandler(SessionDbContext context)
    {
        _context = context;
    }

    public async Task<GetByPublicLinkResult?> Handle(GetByPublicLinkQuery request, CancellationToken cancellationToken)
    {
        var link = await _context.SessionPublicLinks
            .FirstOrDefaultAsync(pl => pl.Token == request.Token, cancellationToken);

        if (link == null) return new GetByPublicLinkResult(null, false, true);

        if (link.RevokedAt != null) return new GetByPublicLinkResult(null, true, false);

        var session = await _context.Sessions.FindAsync(new object[] { link.SessionId }, cancellationToken);
        return session == null
            ? new GetByPublicLinkResult(null, false, true)
            : new GetByPublicLinkResult(session, false, false);
    }
}
