public interface IAuthService
{
    Task<AuthResult> LoginAsync(string username, string password);
    Task<AuthResult> RegisterAsync(string username, string password);
    Task<AuthResult> LogoutAsync();
    Task<ApplicationUser?> GetCurrentUserAsync();
}