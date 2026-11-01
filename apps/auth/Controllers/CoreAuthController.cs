using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/auth")]
public class CoreAuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public CoreAuthController(IAuthService authService, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _authService = authService;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginRequest request)
    {
        if (!Url.IsLocalUrl(request.ReturnUrl))
        {
            return BadRequest();
        }

        var result = await _signInManager.PasswordSignInAsync(request.UserName, request.Password, request.RememberMe, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            return Unauthorized(new { Message = "Invalid login attempt." });
        }

        return LocalRedirect(request.ReturnUrl);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromForm] UserRegisterRequest request, [FromForm] string returnUrl)
    {
        if (!Url.IsLocalUrl(returnUrl))
        {
            return BadRequest();
        }

        var result = await _authService.RegisterAsync(request);
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return LocalRedirect(returnUrl);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var result = await _authService.LogoutAsync();
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var user = await _authService.GetCurrentUserAsync(User);
        if (user is null)
        {
            return NotFound();
        }
        return Ok(new
        {
            user.Id,
            user.UserName,
            user.Email
        });
    }
}