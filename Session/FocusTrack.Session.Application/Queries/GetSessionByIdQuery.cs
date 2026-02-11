using FocusTrack.Session.Domain.Models;
using MediatR;

namespace FocusTrack.Session.Application.Queries;

public record GetSessionByIdQuery(Guid SessionId, string? UserId) : IRequest<Domain.Models.Session?>;
