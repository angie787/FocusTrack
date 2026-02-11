using System.Collections.Concurrent;

namespace FocusTrack.Notification.Api.Services;

public class ConnectionTracker : IConnectionTracker
{
    private readonly ConcurrentDictionary<string, string> _connectionToUser = new();
    private readonly ConcurrentDictionary<string, int> _userConnectionCount = new();

    public void Add(string userId, string connectionId)
    {
        _connectionToUser[connectionId] = userId;
        _userConnectionCount.AddOrUpdate(userId, 1, (_, count) => count + 1);
    }

    public void Remove(string connectionId)
    {
        if (_connectionToUser.TryRemove(connectionId, out var userId) && !string.IsNullOrEmpty(userId))
            _userConnectionCount.AddOrUpdate(userId, 0, (_, count) => Math.Max(0, count - 1));
    }

    public bool IsConnected(string userId)
    {
        return _userConnectionCount.TryGetValue(userId, out var count) && count > 0;
    }
}
