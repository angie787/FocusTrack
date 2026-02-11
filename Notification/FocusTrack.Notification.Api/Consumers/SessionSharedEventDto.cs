namespace FocusTrack.Notification.Api.Consumers;

public record SessionSharedEventDto(Guid SessionId, string OwnerUserId, List<string> RecipientUserIds, DateTimeOffset SharedAt);
