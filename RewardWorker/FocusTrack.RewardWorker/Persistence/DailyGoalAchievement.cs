namespace FocusTrack.RewardWorker.Persistence;

public class DailyGoalAchievement
{
    public long Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateOnly CalendarDate { get; set; }
    public Guid TriggeringSessionId { get; set; }
    public DateTimeOffset AchievedAt { get; set; }
}
