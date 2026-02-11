using FocusTrack.Session.Application.DTOs;
using MediatR;

namespace FocusTrack.Session.Application.Commands;

public record CreatePublicLinkCommand(Guid SessionId, string UserId, string BaseUrl) : IRequest<CreatePublicLinkResponse?>;
