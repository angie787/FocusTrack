using System.Net.Http.Headers;

namespace FocusTrack.RewardWorker.Services;

public class SessionApiClient : ISessionApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<SessionApiClient> _logger;

    public SessionApiClient(HttpClient http, ILogger<SessionApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<bool> SetDailyGoalAchievedAsync(Guid sessionId, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"api/sessions/{sessionId}/daily-goal-achieved");
        // ApiKey is set by HttpClient factory / options
        var response = await _http.SendAsync(request, ct);
        if (response.IsSuccessStatusCode) return true;
        _logger.LogWarning("Session API returned {StatusCode} for PATCH daily-goal-achieved {SessionId}", response.StatusCode, sessionId);
        return false;
    }
}
