using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusTrack.Session.Domain.Events
{
    public record SessionCreatedEvent(Guid SessionId, string UserId, string Topic, DateTimeOffset StartTime);
    public record SessionUpdatedEvent(Guid SessionId, string UserId, string Topic, DateTimeOffset? EndTime, DateTimeOffset StartTime, decimal DurationMin);
    public record SessionDeletedEvent(Guid SessionId, string UserId);

    //Published when a session is shared with users. Notification service consumes for SignalR + email
    public record SessionSharedEvent(Guid SessionId, string OwnerUserId, IReadOnlyList<string> RecipientUserIds, DateTimeOffset SharedAt);

    //Published when an admin changes user status
    public record UserStatusChangedEvent(string UserId, string? OldStatus, string NewStatus, string ChangedBy, DateTimeOffset ChangedAt);
}
