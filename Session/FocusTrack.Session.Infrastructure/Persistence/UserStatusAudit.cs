namespace FocusTrack.Session.Infrastructure.Persistence;

//Audit record for every user status change
public class UserStatusAudit
{
    public long Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
    public DateTimeOffset ChangedAt { get; set; }
}
