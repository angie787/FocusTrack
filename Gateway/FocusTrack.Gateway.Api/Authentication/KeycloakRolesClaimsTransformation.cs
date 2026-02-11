using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text.Json;

namespace FocusTrack.Gateway.Api.Authentication;

//Maps Keycloak realm roles (from realm_access.roles in the token) to ClaimTypes.Role
//so that RequireRole("Admin") and User.IsInRole("Admin") work
//Reads only from principal claims (realm_access is present when JWT Bearer or OIDC add it)
public class KeycloakRolesClaimsTransformation : IClaimsTransformation
{
    private const string RealmAccessClaimType = "realm_access";

    public KeycloakRolesClaimsTransformation()
    {
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return principal;

        if (principal.HasClaim(c => c.Type == ClaimTypes.Role))
            return principal;

        // Only read from principal; do not call GetTokenAsync here — it re-enters auth and causes StackOverflowException
        var realmAccess = principal.FindFirst(RealmAccessClaimType)?.Value;

        if (string.IsNullOrEmpty(realmAccess))
            return principal;

        List<Claim>? roleClaims = null;
        try
        {
            using var doc = JsonDocument.Parse(realmAccess);
            var root = doc.RootElement;
            if (root.TryGetProperty("roles", out var rolesEl) && rolesEl.ValueKind == JsonValueKind.Array)
            {
                roleClaims = new List<Claim>();
                foreach (var roleEl in rolesEl.EnumerateArray())
                {
                    var role = roleEl.GetString();
                    if (!string.IsNullOrEmpty(role))
                        roleClaims.Add(new Claim(ClaimTypes.Role, role));
                }
            }
        }
        catch
        {
            // ignore parse errors
        }

        if (roleClaims is { Count: > 0 } && principal.Identity is ClaimsIdentity identity)
        {
            var newIdentity = new ClaimsIdentity(identity.Claims, identity.AuthenticationType, identity.NameClaimType, ClaimTypes.Role);
            foreach (var claim in roleClaims)
                newIdentity.AddClaim(claim);
            return new ClaimsPrincipal(newIdentity);
        }

        return principal;
    }
}
