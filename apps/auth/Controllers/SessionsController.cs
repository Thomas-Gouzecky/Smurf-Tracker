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

    public SessionsController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IOpenIddictAuthorizationManager authorizationManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _authorizationManager = authorizationManager;
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

        // Revoke all OpenIddict authorizations for the user
        await _authorizationManager.RevokeBySubjectAsync(userId, cancellationToken);

        // Sign out the current session
        await _signInManager.SignOutAsync();

        return Ok(new { Message = "All sessions have been logged out successfully." });
    }

    [HttpGet("sessions")]
    [Authorize]
    public async Task<IActionResult> GetActiveSessions()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return NotFound();

        // I think this needs to get the sessions from oidc

        return Ok(activeSessions);
    }

    [HttpDelete("sessions/{sessionId}")]
    [Authorize]
    public async Task<IActionResult> DeleteSession(string sessionId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return NotFound();

        // I think this needs to delete the session from oidc

        return Ok(new { Message = "Session deleted successfully." });
    }
}