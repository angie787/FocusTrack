using FocusTrack.Session.Application.Commands;
using FocusTrack.Session.Application.DTOs;
using FocusTrack.Session.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FocusTrack.Session.Infrastructure.Handlers;

public sealed class CreatePublicLinkCommandHandler : IRequestHandler<CreatePublicLinkCommand, CreatePublicLinkResponse?>
{
    private readonly SessionDbContext _context;

    public CreatePublicLinkCommandHandler(SessionDbContext context)
    {
        _context = context;
    }

    public async Task<CreatePublicLinkResponse?> Handle(CreatePublicLinkCommand request, CancellationToken cancellationToken)
    {
        var session = await _context.Sessions.FindAsync(new object[] { request.SessionId }, cancellationToken);
        if (session == null || session.UserId != request.UserId) return null;

        var token = Guid.NewGuid().ToString("N");
        var url = $"{request.BaseUrl.TrimEnd('/')}/api/sessions/public/{token}";

        _context.SessionPublicLinks.Add(new SessionPublicLink
        {
            SessionId = request.SessionId,
            Token = token,
            CreatedAt = DateTimeOffset.UtcNow
        });
        _context.SessionShareAudits.Add(new SessionShareAudit
        {
            SessionId = request.SessionId,
            PerformedByUserId = request.UserId,
            Action = "PublicLinkCreated",
            Details = token,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken);

        return new CreatePublicLinkResponse(url);
    }
}
