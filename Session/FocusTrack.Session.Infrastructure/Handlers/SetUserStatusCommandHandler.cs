using FocusTrack.Session.Application.Commands;
using FocusTrack.Session.Application.Common;
using FocusTrack.Session.Domain.Events;
using FocusTrack.Session.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FocusTrack.Session.Infrastructure.Handlers;

public sealed class SetUserStatusCommandHandler : IRequestHandler<SetUserStatusCommand, bool>
{
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase) { "Active", "Suspended", "Deactivated" };

    private readonly SessionDbContext _context;
    private readonly IMediator _mediator;

    public SetUserStatusCommandHandler(SessionDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    public async Task<bool> Handle(SetUserStatusCommand request, CancellationToken cancellationToken)
    {
        var status = request.Request.Status?.Trim();
        if (string.IsNullOrEmpty(status) || !ValidStatuses.Contains(status)) return false;

        var userId = request.UserId;
        var existing = await _context.UserStatuses.FindAsync(new object[] { userId }, cancellationToken);
        var oldStatus = existing?.Status;
        var now = DateTimeOffset.UtcNow;

        if (existing != null)
        {
            existing.Status = status;
            existing.UpdatedAt = now;
            existing.UpdatedBy = request.ChangedBy;
        }
        else
        {
            _context.UserStatuses.Add(new UserStatus
            {
                UserId = userId,
                Status = status,
                UpdatedAt = now,
                UpdatedBy = request.ChangedBy
            });
        }

        _context.UserStatusAudits.Add(new UserStatusAudit
        {
            UserId = userId,
            OldStatus = oldStatus,
            NewStatus = status,
            ChangedBy = request.ChangedBy,
            ChangedAt = now
        });

        await _mediator.Publish(new DomainEventNotification<UserStatusChangedEvent>(
            new UserStatusChangedEvent(userId, oldStatus, status, request.ChangedBy, now)), cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
