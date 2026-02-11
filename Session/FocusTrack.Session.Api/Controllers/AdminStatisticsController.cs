using FocusTrack.Session.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FocusTrack.Session.Api.Controllers;

//Monthly focus-time statistics. Admins see total focused minutes per user per month (CQRS read model)
[ApiController]
[Route("admin/statistics")]
public class AdminStatisticsController : ControllerBase
{
    private readonly SessionDbContext _context;

    public AdminStatisticsController(SessionDbContext context)
    {
        _context = context;
    }

    //GET /admin/statistics/monthly-focus. Returns { UserId, Year, Month, TotalDurationMin }. Supports Page, PageSize, OrderBy (UserId or TotalDurationMin), Direction
    [HttpGet("monthly-focus")]
    public async Task<IActionResult> GetMonthlyFocus(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string orderBy = "UserId",
        [FromQuery] string direction = "asc")
    {
        if (!IsAdmin())
            return Forbid();

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.MonthlyFocusSummaries.AsNoTracking();

        var totalCount = await query.CountAsync();

        var isDesc = string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase);
        var ordered = orderBy.ToLowerInvariant() switch
        {
            "totaldurationmin" => isDesc
                ? query.OrderByDescending(s => s.TotalDurationMin)
                : query.OrderBy(s => s.TotalDurationMin),
            _ => isDesc
                ? query.OrderByDescending(s => s.UserId).ThenByDescending(s => s.Year).ThenByDescending(s => s.Month)
                : query.OrderBy(s => s.UserId).ThenBy(s => s.Year).ThenBy(s => s.Month)
        };

        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new { s.UserId, s.Year, s.Month, s.TotalDurationMin })
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
