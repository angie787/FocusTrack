namespace FocusTrack.Gateway.Api.Services;

public class OidcRevokeService : IOidcRevokeService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OidcRevokeService> _logger;

    public OidcRevokeService(HttpClient httpClient, IConfiguration configuration, ILogger<OidcRevokeService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task RevokeRefreshTokenAsync(string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;

        var authority = _configuration["Oidc:Authority"]?.TrimEnd('/');
        var clientId = _configuration["Oidc:ClientId"];
        var clientSecret = _configuration["Oidc:ClientSecret"];
        if (string.IsNullOrEmpty(authority) || string.IsNullOrEmpty(clientId)) return;

        var revokeUrl = $"{authority}/protocol/openid-connect/revoke";
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["token"] = refreshToken,
            ["token_type_hint"] = "refresh_token",
            ["client_id"] = clientId!,
            ["client_secret"] = clientSecret ?? ""
        });

        try
        {
            var response = await _httpClient.PostAsync(revokeUrl, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("OIDC revoke returned {StatusCode} for refresh token", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OIDC revoke request failed");
        }
    }
}
