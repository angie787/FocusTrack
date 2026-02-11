namespace FocusTrack.Session.Infrastructure.Persistence;

//CQRS read model: total focus minutes per user per month, updated by SessionCreated/Updated/Deleted
public class MonthlyFocusSummary
{
    public long Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalDurationMin { get; set; }
}
