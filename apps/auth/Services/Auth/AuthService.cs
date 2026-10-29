using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

public sealed class AuthService : IAuthService
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthService(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    public async Task<AuthResult> LoginAsync(string username, string password)
    {
        var user = await _userManager.FindByNameAsync(username);
        if (user is null)
        {
            return AuthResult.Failure(new[]
            {
                new AuthError("UserNotFound", "User not found", "username")
            });
        }

        var result = await _signInManager.PasswordSignInAsync(
            username,
            password,
            isPersistent: false,
            lockoutOnFailure: false);

        return result.Succeeded
            ? AuthResult.Success(user.Id, user.UserName ?? username)
            : AuthResult.Failure(new[]
            {
                new AuthError("InvalidCredentials", "Invalid Credentials", "password")
            });
    }

    public async Task<AuthResult> RegisterAsync(string username, string password)
    {
        var user = new ApplicationUser { UserName = username };
        var result = await _userManager.CreateAsync(user, password);

        return result.Succeeded
            ? AuthResult.Success()
            : AuthResult.Failure(result.Errors.Select(error =>
                new AuthError(
                    error.Code,
                    error.Description,
                    GetInputField(error.Code))));
    }

    private static string GetInputField(string errorCode)
    {
        if (!string.IsNullOrEmpty(errorCode) &&
            errorCode.Contains("UserName", StringComparison.OrdinalIgnoreCase))
        {
            return "username";
        }

        if (!string.IsNullOrEmpty(errorCode) &&
            errorCode.Contains("Password", StringComparison.OrdinalIgnoreCase))
        {
            return "password";
        }

        return "general";
    }

    public async Task<AuthResult> LogoutAsync()
    {
        await _signInManager.SignOutAsync();
        return AuthResult.Success();
    }

    public async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        return await _userManager.GetUserAsync(_signInManager.Context.User);
    }
}