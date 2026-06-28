using Bobeta.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Bobeta.Application.Services;

/// <summary>Hashes and verifies portal user passwords using ASP.NET Core Identity's PBKDF2 hasher.</summary>
public sealed class PortalPasswordHasher
{
  private readonly PasswordHasher<PortalUser> _hasher = new();

  public string Hash(PortalUser user, string password) => _hasher.HashPassword(user, password);

  public bool Verify(PortalUser user, string password)
  {
    var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
    return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
  }
}
