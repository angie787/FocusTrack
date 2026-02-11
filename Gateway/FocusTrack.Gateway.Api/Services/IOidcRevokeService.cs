namespace FocusTrack.Gateway.Api.Services;

public interface IOidcRevokeService
{
    //Revokes the refresh token with the OIDC provider (Keycloak) so it cannot be used again
    Task RevokeRefreshTokenAsync(string? refreshToken, CancellationToken cancellationToken = default);
}
