namespace FocusTrack.Gateway.Api.Services;

public interface IOidcTokenRefreshService
{
    //If refresh token is valid, silently refresh the access token when it has expired
    Task<string?> GetValidAccessTokenAsync(HttpContext context, CancellationToken cancellationToken = default);
}
