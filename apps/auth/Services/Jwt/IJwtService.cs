public interface IJwtService
{
    string CreateAccessToken(ApplicationUser user);
}