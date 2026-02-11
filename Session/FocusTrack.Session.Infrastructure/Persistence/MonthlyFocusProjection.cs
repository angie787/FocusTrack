using FocusTrack.Session.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FocusTrack.Session.Infrastructure.Persistence;

//Recomputes MonthlyFocusSummary for a (userId, year, month) from Sessions. Called after Session create/update/delete
public class MonthlyFocusProjection : IMonthlyFocusProjection
{
    private readonly SessionDbContext _context;

    public MonthlyFocusProjection(SessionDbContext context)
    {
        _context = context;
    }

    public async Task RecomputeForUserAndMonthAsync(string userId, int year, int month, CancellationToken ct = default)
    {
        var startOfMonth = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
        var endOfMonth = startOfMonth.AddMonths(1);

        var total = await _context.Sessions
            .Where(s => s.UserId == userId && s.StartTime >= startOfMonth && s.StartTime < endOfMonth)
            .SumAsync(s => s.DurationMin, ct);

        var existing = await _context.MonthlyFocusSummaries
            .FirstOrDefaultAsync(m => m.UserId == userId && m.Year == year && m.Month == month, ct);

        if (existing != null)
        {
            existing.TotalDurationMin = total;
            if (total == 0)
                _context.MonthlyFocusSummaries.Remove(existing);
        }
        else if (total > 0)
        {
            _context.MonthlyFocusSummaries.Add(new MonthlyFocusSummary
            {
                UserId = userId,
                Year = year,
                Month = month,
                TotalDurationMin = total
            });
        }

        await _context.SaveChangesAsync(ct);
    }
}
