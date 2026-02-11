using FocusTrack.Session.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FocusTrack.Session.Api.Controllers;

//Internal: Gateway calls GET /internal/users/{id}/status to check if user can authenticate
//Suspended/Deactivated users must not successfully authenticate
[ApiController]
[Route("internal/users")]
public class InternalUsersController : ControllerBase
{
    private readonly SessionDbContext _context;

    public InternalUsersController(SessionDbContext context)
    {
        _context = context;
    }

    //GET /internal/users/{id}/status. Returns { "status": "Active"|"Suspended"|"Deactivated" }. Secured by X-Internal-Api-Key
    [HttpGet("{id}/status")]
    public async Task<IActionResult> GetStatus(string id)
    {
        var config = HttpContext.RequestServices.GetService<IConfiguration>();
        var expectedKey = config?["InternalApi:ApiKey"];
        var apiKey = Request.Headers["X-Internal-Api-Key"].FirstOrDefault();
        if (string.IsNullOrEmpty(expectedKey) || apiKey != expectedKey)
            return Unauthorized();

        var userStatus = await _context.UserStatuses.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == id);

        var status = userStatus?.Status ?? "Active";
        return Ok(new { status });
    }
}
