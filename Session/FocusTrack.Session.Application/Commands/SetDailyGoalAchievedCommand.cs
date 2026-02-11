using MediatR;

namespace FocusTrack.Session.Application.Commands;

//Internal command called by RewardWorker
public record SetDailyGoalAchievedCommand(Guid SessionId) : IRequest<bool>;
