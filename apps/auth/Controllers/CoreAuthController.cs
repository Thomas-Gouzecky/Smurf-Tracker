using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/auth")]
public class CoreAuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IOutboxService _outboxService;

    public CoreAuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext dbContext, IOutboxService outboxService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
        _outboxService = outboxService;
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
    public async Task<IActionResult> Register([FromForm] UserRegisterRequest request, [FromForm] string returnUrl, CancellationToken cancellationToken = default)
    {
        if (!Url.IsLocalUrl(returnUrl))
        {
            return BadRequest();
        }

        var user = new ApplicationUser { UserName = request.UserName, Email = request.Email, PhoneNumber = request.PhoneNumber };

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return BadRequest(result);
        }

        await _outboxService.AddMessageAsync(new OutboxMessage
        {
            AggregateId = user.Id,
            AggregateType = nameof(ApplicationUser),
            EventType = "UserRegistered",
            Payload = JsonDocument.Parse(JsonSerializer.Serialize(new UserRegisteredEvent { Id = user.Id, UserName = user.UserName, Email = user.Email })),
        }, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        await _signInManager.SignInAsync(user, isPersistent: false);

        return LocalRedirect(returnUrl);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var user = await _userManager.GetUserAsync(User);
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