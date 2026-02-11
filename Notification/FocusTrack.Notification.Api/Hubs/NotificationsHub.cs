using FocusTrack.Notification.Api.Services;
using Microsoft.AspNetCore.SignalR;

namespace FocusTrack.Notification.Api.Hubs;

//Clients connect with ?userId=xxx and join group "user_{userId}". Used to push session-shared notifications to online users
public class NotificationsHub : Hub
{
    private readonly IConnectionTracker _tracker;

    public NotificationsHub(IConnectionTracker tracker)
    {
        _tracker = tracker;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.GetHttpContext()?.Request.Query["userId"].FirstOrDefault();
        if (!string.IsNullOrEmpty(userId))
        {
            _tracker.Add(userId, Context.ConnectionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, "user_" + userId);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _tracker.Remove(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
