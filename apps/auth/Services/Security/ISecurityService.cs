public interface ISecurityService
{
    Task<bool> ForgotPasswordAsync(string email);
    Task<bool> ResetPasswordAsync(string email, string resetToken, string newPassword);
    Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    Task<bool> VerifyEmailAsync(string userId, string verificationCode);
    Task<bool> ResendVerificationEmailAsync(string userId);
}