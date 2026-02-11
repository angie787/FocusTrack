using FocusTrack.Session.Application.Queries;
using FocusTrack.Session.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FocusTrack.Session.Infrastructure.Handlers;

public sealed class GetSessionByIdQueryHandler : IRequestHandler<GetSessionByIdQuery, Domain.Models.Session?>
{
    private readonly SessionDbContext _context;

    public GetSessionByIdQueryHandler(SessionDbContext context)
    {
        _context = context;
    }

    public async Task<Domain.Models.Session?> Handle(GetSessionByIdQuery request, CancellationToken cancellationToken)
    {
        var session = await _context.Sessions.FindAsync(new object[] { request.SessionId }, cancellationToken);
        if (session == null) return null;
        if (string.IsNullOrEmpty(request.UserId)) return null;

        if (session.UserId == request.UserId) return session;
        var shared = await _context.SessionShares
            .AnyAsync(ss => ss.SessionId == request.SessionId && ss.SharedWithUserId == request.UserId, cancellationToken);
        return shared ? session : null;
    }
}
