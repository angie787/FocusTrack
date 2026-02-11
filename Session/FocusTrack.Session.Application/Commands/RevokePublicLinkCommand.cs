using MediatR;

namespace FocusTrack.Session.Application.Commands;

public record RevokePublicLinkCommand(Guid SessionId, string UserId) : IRequest<bool>;
