using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;

[ApiController]
[Route("api/auth")]
public class SessionsController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IOpenIddictAuthorizationManager _authorizationManager;
    private readonly ISessionService _sessionService;

    public SessionsController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IOpenIddictAuthorizationManager authorizationManager, ISessionService sessionService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _authorizationManager = authorizationManager;
        _sessionService = sessionService;
    }

    [HttpPost("logout-all")]
    [Authorize]
    public async Task<IActionResult> LogoutAllSessions(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return NotFound();

        // Invalidate ASP.NET cookie sessions
        await _userManager.UpdateSecurityStampAsync(user);
        await _sessionService.RevokeAllAsync(userId, cancellationToken);

        // Revoke all OpenIddict authorizations for the user
        await _authorizationManager.RevokeBySubjectAsync(userId, cancellationToken);

        // Sign out the current session
        await _signInManager.SignOutAsync();

        return Ok(new { Message = "All sessions have been logged out successfully." });
    }

    [HttpGet("sessions")]
    [Authorize]
    public async Task<IActionResult> GetActiveSessions(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return NotFound();

        var sessions = await _sessionService.GetActiveAsync(userId, cancellationToken);
        return Ok(sessions.Select(session => new
        {
            session.Id,
            session.ClientId,
            session.DeviceName,
            session.IpAddress,
            session.CreatedAt,
            session.LastActivityAt
        }));
    }

    [HttpDelete("sessions/{sessionId}")]
    [Authorize]
    public async Task<IActionResult> DeleteSession(string sessionId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return NotFound();

        if (!Guid.TryParse(sessionId, out var parsedSessionId))
            return BadRequest(new { Message = "The session ID is invalid." });

        if (!await _sessionService.RevokeAsync(
                userId,
                parsedSessionId,
                cancellationToken))
            return NotFound();

        if (ISessionService.GetSessionId(User) == parsedSessionId.ToString())
            await _signInManager.SignOutAsync();

        return Ok(new { Message = "Session deleted successfully." });
    }
}