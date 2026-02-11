namespace FocusTrack.Session.Infrastructure.Persistence;

//User account status for A3. Admins set Active | Suspended | Deactivated
public class UserStatus
{
    public string UserId { get; set; } = string.Empty;
    public string Status { get; set; } = "Active"; // Active | Suspended | Deactivated
    public DateTimeOffset UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}
