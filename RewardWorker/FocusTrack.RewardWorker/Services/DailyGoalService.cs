using FocusTrack.RewardWorker.Events;
using FocusTrack.RewardWorker.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FocusTrack.RewardWorker.Services;

public class DailyGoalService : IDailyGoalService
{
    private const decimal GoalMinutes = 120m;
    private readonly RewardsDbContext _db;
    private readonly ISessionApiClient _sessionApi;
    private readonly IDailyGoalEventPublisher _publisher;
    private readonly ILogger<DailyGoalService> _logger;

    public DailyGoalService(
        RewardsDbContext db,
        ISessionApiClient sessionApi,
        IDailyGoalEventPublisher publisher,
        ILogger<DailyGoalService> logger)
    {
        _db = db;
        _sessionApi = sessionApi;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task OnSessionUpdatedAsync(SessionUpdatedEventDto e, CancellationToken ct = default)
    {
        _logger.LogInformation("Received SessionUpdatedEvent: SessionId={SessionId}, UserId={UserId}, DurationMin={DurationMin}", e.SessionId, e.UserId, e.DurationMin);
        if (e.DurationMin <= 0) return;

        var date = DateOnly.FromDateTime(e.StartTime.UtcDateTime);
        await UpsertContributionAsync(e.UserId, date, e.SessionId, e.DurationMin, ct);

        var total = await GetTotalMinutesForUserAndDateAsync(e.UserId, date, ct);
        var alreadyAchieved = await _db.DailyGoalAchievements
            .AnyAsync(a => a.UserId == e.UserId && a.CalendarDate == date, ct);

        if (total >= GoalMinutes && !alreadyAchieved)
        {
            await AwardDailyGoalAsync(e.UserId, date, e.SessionId, ct);
        }
    }

    public async Task OnSessionDeletedAsync(SessionDeletedEventDto e, CancellationToken ct = default)
    {
        var deleted = await _db.DailyFocusContributions
            .Where(c => c.SessionId == e.SessionId)
            .ExecuteDeleteAsync(ct);
        if (deleted > 0)
            _logger.LogInformation("Removed contribution for deleted session {SessionId}", e.SessionId);
    }

    private async Task UpsertContributionAsync(string userId, DateOnly date, Guid sessionId, decimal durationMin, CancellationToken ct)
    {
        var existing = await _db.DailyFocusContributions
            .FirstOrDefaultAsync(c => c.UserId == userId && c.CalendarDate == date && c.SessionId == sessionId, ct);
        if (existing != null)
        {
            existing.DurationMin = durationMin;
        }
        else
        {
            _db.DailyFocusContributions.Add(new DailyFocusContribution
            {
                UserId = userId,
                CalendarDate = date,
                SessionId = sessionId,
                DurationMin = durationMin
            });
        }
        await _db.SaveChangesAsync(ct);
    }

    private async Task<decimal> GetTotalMinutesForUserAndDateAsync(string userId, DateOnly date, CancellationToken ct)
    {
        return await _db.DailyFocusContributions
            .Where(c => c.UserId == userId && c.CalendarDate == date)
            .SumAsync(c => c.DurationMin, ct);
    }

    private async Task AwardDailyGoalAsync(string userId, DateOnly date, Guid triggeringSessionId, CancellationToken ct)
    {
        var ok = await _sessionApi.SetDailyGoalAchievedAsync(triggeringSessionId, ct);
        if (!ok)
        {
            _logger.LogWarning("Failed to set IsDailyGoalAchieved on session {SessionId}", triggeringSessionId);
            return;
        }

        var achievedAt = DateTimeOffset.UtcNow;
        _db.DailyGoalAchievements.Add(new DailyGoalAchievement
        {
            UserId = userId,
            CalendarDate = date,
            TriggeringSessionId = triggeringSessionId,
            AchievedAt = achievedAt
        });
        await _db.SaveChangesAsync(ct);

        await _publisher.PublishAsync(new DailyGoalAchievedEvent(triggeringSessionId, userId, achievedAt), ct);
        _logger.LogInformation("Daily goal achieved for user {UserId} on {Date}, session {SessionId}", userId, date, triggeringSessionId);
    }
}
