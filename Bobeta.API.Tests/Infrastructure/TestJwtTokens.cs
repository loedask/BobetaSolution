using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Bobeta.API.Tests.Infrastructure;

internal static class TestJwtTokens
{
  private const string Key = "BobetaDefaultSecretKeyForJwtSigningThatIsLongEnough";
  private const string Issuer = "Bobeta";
  private const string Audience = "Bobeta";

  public static string ForPlayer(Guid playerId)
  {
    var credentials = new SigningCredentials(
      new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)),
      SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
      issuer: Issuer,
      audience: Audience,
      claims: [new Claim("playerId", playerId.ToString())],
      expires: DateTime.UtcNow.AddHours(1),
      signingCredentials: credentials);

    return new JwtSecurityTokenHandler().WriteToken(token);
  }
}
