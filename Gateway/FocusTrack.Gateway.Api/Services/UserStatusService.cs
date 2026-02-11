using System.Text.Json;

namespace FocusTrack.Gateway.Api.Services;

public class UserStatusService : IUserStatusService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserStatusService> _logger;

    public UserStatusService(HttpClient httpClient, ILogger<UserStatusService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string?> GetStatusAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"internal/users/{Uri.EscapeDataString(userId)}/status", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("User status check returned {StatusCode} for user {UserId}", response.StatusCode, userId);
                return null;
            }
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("status", out var statusProp))
                return statusProp.GetString();
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "User status check failed for user {UserId}", userId);
            return null;
        }
    }
}
