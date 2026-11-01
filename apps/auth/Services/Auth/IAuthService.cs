using System.Security.Claims;
using Microsoft.AspNetCore.Identity.Data;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(LoginRequest request);
    Task<AuthResult> RegisterAsync(UserRegisterRequest request);
    Task<AuthResult> LogoutAsync();
    Task<ApplicationUser?> GetCurrentUserAsync(ClaimsPrincipal userClaims);
}