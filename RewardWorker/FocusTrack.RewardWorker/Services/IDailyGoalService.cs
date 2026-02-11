using FocusTrack.RewardWorker.Events;

namespace FocusTrack.RewardWorker.Services;

public interface IDailyGoalService
{
    Task OnSessionUpdatedAsync(SessionUpdatedEventDto e, CancellationToken ct = default);
    Task OnSessionDeletedAsync(SessionDeletedEventDto e, CancellationToken ct = default);
}
