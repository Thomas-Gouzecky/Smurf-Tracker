using Microsoft.AspNetCore.Identity;

public class SecurityService : ISecurityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailServiceClient _emailServiceClient;

    public SecurityService(UserManager<ApplicationUser> userManager, IEmailServiceClient emailServiceClient)
    {
        _userManager = userManager;
        _emailServiceClient = emailServiceClient;
    }

    /// <summary>
    /// Initiates the forgot password process for a user by generating a password reset token and sending a password reset email. It first attempts to find the user by their email address. If the user is not found, it returns false. If the user is found, it generates a password reset token using UserManager and constructs a password reset URL. It then sends an email to the user with the password reset link using the IEmailServiceClient. The method returns true if the email was sent successfully, otherwise it returns false.
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public async Task<bool> ForgotPasswordAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return false;
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = System.Web.HttpUtility.UrlEncode(token);
        var encodedUserId = System.Web.HttpUtility.UrlEncode(user.Id);
        var resetUrl = $"https://yourapp.com/reset-password?userId={encodedUserId}&token={encodedToken}";

        var result = await _emailServiceClient.SendEmailAsync(new SendEmailRequest
        {
            To = email,
            template = "password_reset",
            data = new { ResetUrl = resetUrl }
        });
        return result;
    }

    /// <summary>
    /// Resets the password for a user by validating the provided reset token and setting the new password. It first attempts to find the user by their email address. If the user is not found, it returns false. If the user is found, it calls the ResetPasswordAsync method of UserManager to reset the password using the provided reset token and new password. The method returns true if the password reset is successful, otherwise it returns false.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="resetToken"></param>
    /// <param name="newPassword"></param>
    /// <returns></returns>
    public async Task<bool> ResetPasswordAsync(string email, string resetToken, string newPassword)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return false;
        }

        var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);
        return result.Succeeded;
    }

    /// <summary>
    /// Changes the password for a user by validating the current password and setting the new password. It first attempts to find the user by their ID. If the user is not found, it returns false. If the user is found, it calls the ChangePasswordAsync method of UserManager to change the password using the provided current password and new password. The method returns true if the password change is successful, otherwise it returns false.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="currentPassword"></param>
    /// <param name="newPassword"></param>
    /// <returns></returns>
    public async Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        return result.Succeeded;
    }

    /// <summary>
    /// Verifies the email of a user by confirming the email using the provided verification code. It first attempts to find the user by their ID. If the user is not found, it returns false. If the user is found, it calls the ConfirmEmailAsync method of UserManager to confirm the email with the provided verification code. The method returns true if the email confirmation is successful, otherwise it returns false.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="verificationCode"></param>
    /// <returns></returns>
    public async Task<bool> VerifyEmailAsync(string userId, string verificationCode)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        var result = await _userManager.ConfirmEmailAsync(user, verificationCode);
        return result.Succeeded;
    }

    /// <summary>
    /// Resends the email verification link to a user. It first attempts to find the user by their ID. If the user is not found, it returns false. If the user is found, it generates a new email confirmation token using UserManager and constructs a verification URL. It then sends an email to the user with the verification link using the IEmailServiceClient. The method returns true if the email was sent successfully, otherwise it returns false.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<bool> ResendVerificationEmailAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        if (user.EmailConfirmed)
        {
            return true; // Email is already confirmed
        }

        var encodedUserId = System.Web.HttpUtility.UrlEncode(user.Id);
        var encodedToken = System.Web.HttpUtility.UrlEncode(await _userManager.GenerateEmailConfirmationTokenAsync(user));
        var verificationUrl = $"https://yourapp.com/verify-email?userId={encodedUserId}&token={encodedToken}";

        var result = await _emailServiceClient.SendEmailAsync(new SendEmailRequest
        {
            To = user.Email!,
            template = "email_verification",
            data = new { VerificationUrl = verificationUrl }
        });
        return result;
    }
}