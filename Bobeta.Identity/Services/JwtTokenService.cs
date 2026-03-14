using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Bobeta.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Bobeta.Identity.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration) => _configuration = configuration;

    public string GenerateToken(Guid playerId, string playerName)
    {
        var key = _configuration["Jwt:Key"] ?? "BobetaDefaultSecretKeyForJwtSigningThatIsLongEnough";
        var issuer = _configuration["Jwt:Issuer"] ?? "Bobeta";
        var audience = _configuration["Jwt:Audience"] ?? "Bobeta";
        var expiryMinutes = int.TryParse(_configuration["Jwt:ExpiryMinutes"], out var m) ? m : 60;

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, playerId.ToString()),
            new Claim(ClaimTypes.Name, playerName),
            new Claim("playerId", playerId.ToString())
        };
        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
