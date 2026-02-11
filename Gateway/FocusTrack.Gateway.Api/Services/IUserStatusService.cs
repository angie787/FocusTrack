namespace FocusTrack.Gateway.Api.Services;

public interface IUserStatusService
{
    Task<string?> GetStatusAsync(string userId, CancellationToken cancellationToken = default);
}
