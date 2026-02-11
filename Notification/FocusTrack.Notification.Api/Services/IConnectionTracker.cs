namespace FocusTrack.Notification.Api.Services;

//Tracks which user IDs are currently connected to SignalR so we can send realtime vs email
public interface IConnectionTracker
{
    void Add(string userId, string connectionId);
    void Remove(string connectionId);
    bool IsConnected(string userId);
}
