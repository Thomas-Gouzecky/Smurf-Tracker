using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/auth")]
public class SecurityController : ControllerBase
{
    private readonly ISecurityService _securityService;

    public SecurityController(ISecurityService securityService)
    {
        _securityService = securityService;
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _securityService.ForgotPasswordAsync(request.Email);

        return Ok(new { Message = "If an account with that email exists, a password reset link has been sent." });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await _securityService.ResetPasswordAsync(request.Email, request.ResetCode, request.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { Message = "Failed to reset password.", Errors = result.Errors });

        return Ok(new { Message = "Password has been reset successfully." });
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
            return Unauthorized();

        var result = await _securityService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { Message = "Failed to change password.", Errors = result.Errors });

        return Ok(new { Message = "Password has been changed successfully." });
    }

    [HttpPost("verify-email")]
    [Authorize]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
            return Unauthorized();

        var result = await _securityService.VerifyEmailAsync(userId, request.VerificationCode);
        if (!result.Succeeded)
            return BadRequest(new { Message = "Failed to verify email.", Errors = result.Errors });

        return Ok(new { Message = "Email has been verified successfully." });
    }

    [HttpPost("resend-verification-email")]
    [Authorize]
    public async Task<IActionResult> ResendVerificationEmail()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
            return Unauthorized();

        var result = await _securityService.ResendVerificationEmailAsync(userId);
        if (!result)
            return BadRequest(new { Message = "Failed to resend verification email." });

        return Ok(new { Message = "Verification email has been resent successfully." });
    }
}