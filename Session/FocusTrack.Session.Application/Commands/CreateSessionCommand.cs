using FocusTrack.Session.Application.DTOs;
using FocusTrack.Session.Domain.Models;
using MediatR;

namespace FocusTrack.Session.Application.Commands;

public record CreateSessionCommand(CreateSessionRequest Request, string UserId) : IRequest<CreateSessionResult>;

public record CreateSessionResult(Domain.Models.Session Session);
