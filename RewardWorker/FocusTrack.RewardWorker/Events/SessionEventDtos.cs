namespace FocusTrack.RewardWorker.Events;

//DTO matching Session service's SessionCreatedEvent JSON
public record SessionCreatedEventDto(Guid SessionId, string UserId, string Topic, DateTimeOffset StartTime);

//DTO matching Session service's SessionUpdatedEvent JSON
public record SessionUpdatedEventDto(Guid SessionId, string UserId, string Topic, DateTimeOffset? EndTime, DateTimeOffset StartTime, decimal DurationMin);

//DTO matching Session service's SessionDeletedEvent JSON
public record SessionDeletedEventDto(Guid SessionId, string UserId);

//Published by RewardWorker when user first crosses 120 min for the day
public record DailyGoalAchievedEvent(Guid SessionId, string UserId, DateTimeOffset AchievedAt);
