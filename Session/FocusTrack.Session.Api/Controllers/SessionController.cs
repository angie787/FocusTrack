using FocusTrack.Session.Api.Services;
using FocusTrack.Session.Application.Commands;
using FocusTrack.Session.Application.DTOs;
using FocusTrack.Session.Application.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace FocusTrack.Session.Api.Controllers;

[ApiController]
[Route("api/sessions")]
public class SessionController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public SessionController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSessionRequest request)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var result = await _mediator.Send(new CreateSessionCommand(request, userId));
        return CreatedAtAction(nameof(GetById), new { id = result.Session.Id }, result.Session);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var sessions = await _mediator.Send(new GetSessionsQuery(userId, page, pageSize));
        return Ok(sessions);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = _currentUserService.UserId;
        var session = await _mediator.Send(new GetSessionByIdQuery(id, userId));
        if (session == null) return NotFound();
        return Ok(session);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSessionRequest request)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var found = await _mediator.Send(new UpdateSessionCommand(id, request, userId));
        if (!found) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var found = await _mediator.Send(new DeleteSessionCommand(id, userId));
        if (!found) return NotFound();
        return NoContent();
    }

    // Share session with users by ID, recipients see it in their feed. Notifies via SessionSharedEvent
    [HttpPost("{id}/share")]
    public async Task<IActionResult> Share(Guid id, [FromBody] ShareSessionRequest request)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        if (request.UserIds == null || request.UserIds.Count == 0)
            return BadRequest(new { errors = new { UserIds = new[] { "At least one user ID is required." } } });

        var found = await _mediator.Send(new ShareSessionCommand(id, request, userId));
        if (!found) return NotFound();
        return NoContent();
    }

    // Unshare session with a user. Audited
    [HttpDelete("{id}/share/{sharedWithUserId}")]
    public async Task<IActionResult> Unshare(Guid id, string sharedWithUserId)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var found = await _mediator.Send(new UnshareSessionCommand(id, sharedWithUserId, userId));
        if (!found) return NotFound();
        return NoContent();
    }

    // Create a unique, revocable public link for the session
    [HttpPost("{id}/public-link")]
    public async Task<IActionResult> CreatePublicLink(Guid id)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var baseUrl = HttpContext.RequestServices.GetService<IConfiguration>()?["PublicLink:BaseUrl"]
            ?? $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
        var result = await _mediator.Send(new CreatePublicLinkCommand(id, userId, baseUrl));
        if (result == null) return NotFound();
        return Ok(result);
    }

    // Revoke the public link for this session. Future access via link returns 410 Gone
    [HttpPost("{id}/public-link/revoke")]
    public async Task<IActionResult> RevokePublicLink(Guid id)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var found = await _mediator.Send(new RevokePublicLinkCommand(id, userId));
        if (!found) return NotFound();
        return NoContent();
    }

    // Get session by public link token. Returns 410 Gone if link was revoked
    [HttpGet("public/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByPublicLink(string token)
    {
        var result = await _mediator.Send(new GetByPublicLinkQuery(token));
        if (result == null || result.NotFound) return NotFound();
        if (result.IsRevoked) return StatusCode(410, new { message = "This link has been revoked." });
        if (result.Session == null) return NotFound();
        return Ok(result.Session);
    }

    // Internal: called by RewardWorker when user first crosses 120 min focus for the day
    // Secured by X-Internal-Api-Key header.
    [HttpPatch("{id}/daily-goal-achieved")]
    public async Task<IActionResult> SetDailyGoalAchieved(Guid id, [FromHeader(Name = "X-Internal-Api-Key")] string? apiKey)
    {
        var config = HttpContext.RequestServices.GetService<IConfiguration>();
        var expected = config?["InternalApi:ApiKey"];
        if (string.IsNullOrEmpty(expected) || apiKey != expected)
            return Unauthorized();

        var found = await _mediator.Send(new SetDailyGoalAchievedCommand(id));
        if (!found) return NotFound();
        return NoContent();
    }
}
