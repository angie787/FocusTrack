using FocusTrack.Session.Domain.Models;
using MediatR;

namespace FocusTrack.Session.Application.Queries;

public record GetByPublicLinkQuery(string Token) : IRequest<GetByPublicLinkResult?>;

public record GetByPublicLinkResult(Domain.Models.Session? Session, bool IsRevoked, bool NotFound);
