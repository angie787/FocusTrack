using MediatR;

namespace FocusTrack.Session.Application.Commands;

public record DeleteSessionCommand(Guid SessionId, string UserId) : IRequest<bool>;
