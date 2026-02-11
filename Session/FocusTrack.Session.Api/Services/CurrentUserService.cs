namespace FocusTrack.Session.Api.Services
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
    }

    public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
    {
        public string? UserId => httpContextAccessor.HttpContext?.Request.Headers["X-User-Id"];
    }
}
