using FocusTrack.Session.Domain.Models;
using MediatR;

namespace FocusTrack.Session.Application.Queries;

public record GetSessionsQuery(string UserId, int Page, int PageSize) : IRequest<IReadOnlyList<Domain.Models.Session>>;
