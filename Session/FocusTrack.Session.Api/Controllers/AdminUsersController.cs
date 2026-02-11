using FocusTrack.Session.Application.Commands;
using FocusTrack.Session.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FocusTrack.Session.Api.Controllers;

//User management, admins control user account status. Audit and UserStatusChanged event on every change
[ApiController]
[Route("admin/users")]
public class AdminUsersController : ControllerBase
{
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase) { "Active", "Suspended", "Deactivated" };

    private readonly IMediator _mediator;

    public AdminUsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    //PATCH /admin/users/{id}/status with body { "status": "Active|Suspended|Deactivated" }. Audit + UserStatusChanged event
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> SetStatus(string id, [FromBody] SetUserStatusRequest request)
    {
        if (string.IsNullOrEmpty(id)) return BadRequest();
        if (!IsAdmin()) return Forbid();

        var status = request.Status?.Trim();
        if (string.IsNullOrEmpty(status) || !ValidStatuses.Contains(status))
            return BadRequest(new { errors = new { Status = new[] { "Status must be one of: Active, Suspended, Deactivated." } } });

        var changedBy = Request.Headers["X-User-Id"].FirstOrDefault() ?? "unknown";
        var success = await _mediator.Send(new SetUserStatusCommand(id, request, changedBy));
        if (!success) return BadRequest();

        return NoContent();
    }

    private bool IsAdmin()
    {
        var rolesHeader = Request.Headers["X-User-Roles"].FirstOrDefault();
        if (string.IsNullOrEmpty(rolesHeader)) return false;
        var roles = rolesHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return roles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
    }
}
