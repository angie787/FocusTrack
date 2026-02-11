namespace FocusTrack.Notification.Api.Services;

public interface IEmailSender
{
    Task SendSessionSharedNotificationAsync(string recipientUserId, Guid sessionId, string ownerUserId, CancellationToken ct = default);
}
