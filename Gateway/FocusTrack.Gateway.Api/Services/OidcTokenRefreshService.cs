using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace FocusTrack.Gateway.Api.Services;

public class OidcTokenRefreshService : IOidcTokenRefreshService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OidcTokenRefreshService> _logger;

    private const int RefreshBeforeSeconds = 60; // refresh if token expires in less than 60 seconds

    public OidcTokenRefreshService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<OidcTokenRefreshService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string?> GetValidAccessTokenAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        var result = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (result?.Principal == null || !result.Succeeded) return null;

        var accessToken = result.Properties?.GetTokenValue("access_token");
        var refreshToken = result.Properties?.GetTokenValue("refresh_token");
        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken)) return accessToken;

        var expiresAt = result.Properties?.GetTokenValue("expires_at");
        if (!string.IsNullOrEmpty(expiresAt) && DateTimeOffset.TryParse(expiresAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out var expiry))
        {
            if (expiry > DateTimeOffset.UtcNow.AddSeconds(RefreshBeforeSeconds))
                return accessToken; // still valid
        }

        // Refresh tokens
        var authority = _configuration["Oidc:Authority"]?.TrimEnd('/');
        var clientId = _configuration["Oidc:ClientId"];
        var clientSecret = _configuration["Oidc:ClientSecret"];
        if (string.IsNullOrEmpty(authority) || string.IsNullOrEmpty(clientId)) return accessToken;

        var tokenUrl = $"{authority}/protocol/openid-connect/token";
        using var httpClient = _httpClientFactory.CreateClient();
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = clientId!,
            ["client_secret"] = clientSecret ?? "",
            ["refresh_token"] = refreshToken
        });

        HttpResponseMessage? response = null;
        try
        {
            response = await httpClient.PostAsync(tokenUrl, content, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token refresh failed");
            return accessToken; // use existing token even if expired, next request may get 401
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;
        var newAccessToken = root.TryGetProperty("access_token", out var at) ? at.GetString() : null;
        var newRefreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : refreshToken;
        var expiresIn = root.TryGetProperty("expires_in", out var ei) && ei.TryGetInt32(out var sec) ? sec : 300;

        if (string.IsNullOrEmpty(newAccessToken)) return accessToken;

        var newExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn).ToString("o");
        result.Properties!.UpdateTokenValue("access_token", newAccessToken);
        result.Properties.UpdateTokenValue("refresh_token", newRefreshToken ?? refreshToken);
        result.Properties.UpdateTokenValue("expires_at", newExpiresAt);

        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, result.Principal!, result.Properties);
        return newAccessToken;
    }
}
