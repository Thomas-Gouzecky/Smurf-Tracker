using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;

public sealed class AuthService : IAuthService
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtService _jwtService;

    public AuthService(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IJwtService jwtService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _jwtService = jwtService;
    }

    /// <summary>
    /// This method handles the login process for a user. It first attempts to find the user by their email address. If the user is not found, it returns a failure AuthResult with an appropriate error message. If the user is found, it checks the provided password against the stored password hash. If the password is incorrect or if the user account is locked out, it returns a failure AuthResult with the corresponding error messages. If the login is successful, it generates a JWT access token for the user and returns a success AuthResult containing the access token.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return AuthResult.Failure(new[]
            {
                new AuthError("UserNotFound", "User not found", "username")
            });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            return AuthResult.Failure(new[]
            {
                new AuthError("UserLockedOut", "User account is locked out", "username")
            });
        }

        if (!result.Succeeded)
        {
            return AuthResult.Failure(new[]
            {
                new AuthError("InvalidCredentials", "Invalid Credentials", "password")
            });
        }

        var accessToken = _jwtService.CreateAccessToken(user);

        return AuthResult.Success(accessToken);
    }

    /// <summary>
    /// This method handles the registration process for a new user. It creates a new ApplicationUser instance with the provided email and password, and attempts to create the user using the UserManager. If the creation is successful, it returns a success AuthResult. If there are any errors during the creation process, it returns a failure AuthResult with the corresponding error messages.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        var user = new ApplicationUser { UserName = request.Email, Email = request.Email };
        var result = await _userManager.CreateAsync(user, request.Password);

        return result.Succeeded
            ? AuthResult.Success()
            : AuthResult.Failure(result.Errors.Select(error =>
                new AuthError(
                    error.Code,
                    error.Description,
                    GetInputField(error.Code))));
    }

    /// <summary>
    /// This method handles the logout process for the user. In a stateless JWT authentication system, the frontend is responsible for discarding the JWT token, so no server-side action is needed for logout. However, if refresh tokens are implemented in the future, this method should accept the refresh token and revoke it on the server side to prevent further use.
    /// </summary>
    public Task<AuthResult> LogoutAsync()
    {
        // Frontend will discard the JWT token, so no server-side action is needed for logout in a stateless JWT authentication system.
        // Once refresh tokens are implemented, i should accept the refresh token and revoke it on the server side to prevent further use.
        return Task.FromResult(AuthResult.Success());
    }

    /// <summary>
    /// This method retrieves the currently authenticated user based on the claims present in the HttpContext. It uses the UserManager to fetch the user details from the database. If the user is found, it returns the ApplicationUser instance; otherwise, it returns null.
    /// </summary>
    /// <returns></returns>
    /// <param name="userClaims"></param>
    public async Task<ApplicationUser?> GetCurrentUserAsync(ClaimsPrincipal userClaims)
    {
        return await _userManager.GetUserAsync(userClaims);
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
}