using FocusTrack.Session.Application.Queries;
using FocusTrack.Session.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FocusTrack.Session.Infrastructure.Handlers;

public sealed class GetSessionsQueryHandler : IRequestHandler<GetSessionsQuery, IReadOnlyList<Domain.Models.Session>>
{
    private readonly SessionDbContext _context;

    public GetSessionsQueryHandler(SessionDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Domain.Models.Session>> Handle(GetSessionsQuery request, CancellationToken cancellationToken)
    {
        var sharedIds = await _context.SessionShares
            .Where(ss => ss.SharedWithUserId == request.UserId)
            .Select(ss => ss.SessionId)
            .ToListAsync(cancellationToken);

        return await _context.Sessions
            .Where(s => s.UserId == request.UserId || sharedIds.Contains(s.Id))
            .OrderByDescending(s => s.StartTime)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
    }
}
