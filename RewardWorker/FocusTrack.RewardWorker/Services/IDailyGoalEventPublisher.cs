using FocusTrack.RewardWorker.Events;

namespace FocusTrack.RewardWorker.Services;

public interface IDailyGoalEventPublisher
{
    Task PublishAsync(DailyGoalAchievedEvent e, CancellationToken ct = default);
}
