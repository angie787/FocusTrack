using FocusTrack.Session.Application.DTOs;
using MediatR;

namespace FocusTrack.Session.Application.Commands;

public record SetUserStatusCommand(string UserId, SetUserStatusRequest Request, string ChangedBy) : IRequest<bool>;
