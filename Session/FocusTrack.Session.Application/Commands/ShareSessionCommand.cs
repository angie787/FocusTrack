using FocusTrack.Session.Application.DTOs;
using MediatR;

namespace FocusTrack.Session.Application.Commands;

public record ShareSessionCommand(Guid SessionId, ShareSessionRequest Request, string UserId) : IRequest<bool>;
