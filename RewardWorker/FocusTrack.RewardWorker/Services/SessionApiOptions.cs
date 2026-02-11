namespace FocusTrack.RewardWorker.Services;

public class SessionApiOptions
{
    public const string SectionName = "SessionApi";
    public string BaseAddress { get; set; } = "http://session-service:8080/";
    public string InternalApiKey { get; set; } = "";
}
