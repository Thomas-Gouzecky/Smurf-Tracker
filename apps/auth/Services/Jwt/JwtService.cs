using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

public sealed class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreateAccessToken(ApplicationUser user) =>
        CreateAccessToken(user, null);

    public string CreateAccessToken(ApplicationUser user, Guid? sessionId)
    {
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];
        var signingKey = _configuration["Jwt:SigningKey"];

        var securityKey = new SymmetricSecurityKey(Convert.FromBase64String(signingKey!));

        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Name, user.UserName ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
        };

        if (sessionId.HasValue)
        {
            claims = [.. claims, new Claim(
                SessionClaimTypes.SessionId,
                sessionId.Value.ToString())];
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            NotBefore = DateTime.UtcNow,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials,
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}