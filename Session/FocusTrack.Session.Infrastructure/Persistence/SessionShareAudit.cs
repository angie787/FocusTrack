namespace FocusTrack.Session.Infrastructure.Persistence;

public class SessionShareAudit
{
    public long Id { get; set; }
    public Guid SessionId { get; set; }
    public string PerformedByUserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // SharedWithUsers, Unshared, PublicLinkCreated, PublicLinkRevoked
    public string? Details { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
