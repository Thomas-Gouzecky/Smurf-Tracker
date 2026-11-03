public interface IJwtService
{
    string CreateAccessToken(ApplicationUser user);
    string CreateAccessToken(ApplicationUser user, Guid? sessionId);
}