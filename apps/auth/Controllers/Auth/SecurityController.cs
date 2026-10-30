using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("/api/auth")]
public class SecurityController : ControllerBase
{
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        // Implement the logic to handle forgot password functionality
        // For example, generate a password reset token and send it via email
        return Ok(new { Message = "Password reset link has been sent to your email." });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        // Implement the logic to handle reset password functionality
        return Ok(new { Message = "Password has been reset successfully." });
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        // Implement the logic to handle change password functionality
        return Ok(new { Message = "Password has been changed successfully." });
    }

    [HttpPost("verify-email")]
    [Authorize]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        // Implement the logic to handle email verification functionality
        return Ok(new { Message = "Email has been verified successfully." });
    }

    [HttpPost("resend-verification-email")]
    [Authorize]
    public async Task<IActionResult> ResendVerificationEmail([FromBody] ResendVerificationEmailRequest request)
    {
        // Implement the logic to handle resending verification email functionality
        return Ok(new { Message = "Verification email has been resent successfully." });
    }
}