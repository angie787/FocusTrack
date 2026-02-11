namespace FocusTrack.Session.Infrastructure.Persistence;

public class SessionShare
{
    public long Id { get; set; }
    public Guid SessionId { get; set; }
    public string SharedWithUserId { get; set; } = string.Empty;
    public string SharedByUserId { get; set; } = string.Empty;
    public DateTimeOffset SharedAt { get; set; }
}
