namespace FocusTrack.RewardWorker.Services;

public interface ISessionApiClient
{
    Task<bool> SetDailyGoalAchievedAsync(Guid sessionId, CancellationToken ct = default);
}
