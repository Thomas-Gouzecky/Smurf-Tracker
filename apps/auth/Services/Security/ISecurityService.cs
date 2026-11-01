using Microsoft.AspNetCore.Identity;

public interface ISecurityService
{
    Task<bool> ForgotPasswordAsync(string email);
    Task<IdentityResult> ResetPasswordAsync(string email, string resetToken, string newPassword);
    Task<IdentityResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    Task<IdentityResult> VerifyEmailAsync(string userId, string verificationCode);
    Task<bool> ResendVerificationEmailAsync(string userId);
}