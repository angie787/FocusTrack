using MediatR;

namespace FocusTrack.Session.Application.Commands;

public record UnshareSessionCommand(Guid SessionId, string SharedWithUserId, string UserId) : IRequest<bool>;
