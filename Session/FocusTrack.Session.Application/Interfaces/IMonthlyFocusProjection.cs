namespace FocusTrack.Session.Application.Interfaces;

//Updates the monthly focus read model when sessions change (CQRS projection)
public interface IMonthlyFocusProjection
{
    Task RecomputeForUserAndMonthAsync(string userId, int year, int month, CancellationToken ct = default);
}
