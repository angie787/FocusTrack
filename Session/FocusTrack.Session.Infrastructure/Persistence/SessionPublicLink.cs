namespace FocusTrack.Session.Infrastructure.Persistence;

public class SessionPublicLink
{
    public long Id { get; set; }
    public Guid SessionId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
