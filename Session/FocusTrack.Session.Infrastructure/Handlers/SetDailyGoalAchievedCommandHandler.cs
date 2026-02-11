using FocusTrack.Session.Application.Commands;
using FocusTrack.Session.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FocusTrack.Session.Infrastructure.Handlers;

public sealed class SetDailyGoalAchievedCommandHandler : IRequestHandler<SetDailyGoalAchievedCommand, bool>
{
    private readonly SessionDbContext _context;

    public SetDailyGoalAchievedCommandHandler(SessionDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(SetDailyGoalAchievedCommand request, CancellationToken cancellationToken)
    {
        var session = await _context.Sessions.FindAsync(new object[] { request.SessionId }, cancellationToken);
        if (session == null) return false;

        session.IsDailyGoalAchieved = true;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
