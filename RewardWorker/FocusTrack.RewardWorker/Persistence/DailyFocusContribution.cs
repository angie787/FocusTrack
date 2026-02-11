namespace FocusTrack.RewardWorker.Persistence;

public class DailyFocusContribution
{
    public long Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateOnly CalendarDate { get; set; }
    public Guid SessionId { get; set; }
    public decimal DurationMin { get; set; }
}
