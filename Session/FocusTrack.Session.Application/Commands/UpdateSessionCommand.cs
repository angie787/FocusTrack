using FocusTrack.Session.Application.DTOs;
using MediatR;

namespace FocusTrack.Session.Application.Commands;

public record UpdateSessionCommand(Guid SessionId, UpdateSessionRequest Request, string UserId) : IRequest<bool>;
