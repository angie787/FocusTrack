using FocusTrack.Session.Domain.Models;
using FocusTrack.Session.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FocusTrack.Session.Api.Controllers;

[ApiController]
[Route("admin/sessions")]
public class AdminSessionsController : ControllerBase
{
    private readonly SessionDbContext _context;

    public AdminSessionsController(SessionDbContext context)
    {
        _context = context;
    }

    //GET /admin/sessions with filters. Returns paginated results and X-Total-Count header
    [HttpGet]
    public async Task<IActionResult> GetSessions(
        [FromQuery] string? userId,
        [FromQuery] SessionMode? mode,
        [FromQuery] DateTimeOffset? startDateFrom,
        [FromQuery] DateTimeOffset? startDateTo,
        [FromQuery] DateTimeOffset? endDateFrom,
        [FromQuery] DateTimeOffset? endDateTo,
        [FromQuery] decimal? minDuration,
        [FromQuery] decimal? maxDuration,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string orderBy = "StartTime",
        [FromQuery] string direction = "desc")
    {
        if (!IsAdmin())
            return Forbid();

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Sessions.AsNoTracking();

        if (!string.IsNullOrEmpty(userId))
            query = query.Where(s => s.UserId == userId);
        if (mode.HasValue)
            query = query.Where(s => s.Mode == mode.Value);
        if (startDateFrom.HasValue)
            query = query.Where(s => s.StartTime >= startDateFrom.Value);
        if (startDateTo.HasValue)
            query = query.Where(s => s.StartTime <= startDateTo.Value);
        if (endDateFrom.HasValue)
            query = query.Where(s => s.EndTime != null && s.EndTime >= endDateFrom.Value);
        if (endDateTo.HasValue)
            query = query.Where(s => s.EndTime != null && s.EndTime <= endDateTo.Value);
        if (minDuration.HasValue)
            query = query.Where(s => s.DurationMin >= minDuration.Value);
        if (maxDuration.HasValue)
            query = query.Where(s => s.DurationMin <= maxDuration.Value);

        var totalCount = await query.CountAsync();

        var isDesc = string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase);
        query = orderBy.ToLowerInvariant() switch
        {
            "userid" => isDesc ? query.OrderByDescending(s => s.UserId) : query.OrderBy(s => s.UserId),
            "durationmin" => isDesc ? query.OrderByDescending(s => s.DurationMin) : query.OrderBy(s => s.DurationMin),
            "topic" => isDesc ? query.OrderByDescending(s => s.Topic) : query.OrderBy(s => s.Topic),
            "mode" => isDesc ? query.OrderByDescending(s => s.Mode) : query.OrderBy(s => s.Mode),
            "starttime" => isDesc ? query.OrderByDescending(s => s.StartTime) : query.OrderBy(s => s.StartTime),
            "endtime" => isDesc ? query.OrderByDescending(s => s.EndTime) : query.OrderBy(s => s.EndTime),
            _ => isDesc ? query.OrderByDescending(s => s.StartTime) : query.OrderBy(s => s.StartTime)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        return Ok(items);
    }

    private bool IsAdmin()
    {
        var rolesHeader = Request.Headers["X-User-Roles"].FirstOrDefault();
        if (string.IsNullOrEmpty(rolesHeader)) return false;
        var roles = rolesHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return roles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
    }
}
