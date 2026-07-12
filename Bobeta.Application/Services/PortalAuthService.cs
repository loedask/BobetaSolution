using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;

namespace Bobeta.Application.Services;

public sealed class PortalAuthService(IPortalUserRepository portalUsers, PortalPasswordHasher passwordHasher) : IPortalAuthService
{
  public async Task<PortalUser?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
      return null;

    var user = await portalUsers.GetByEmailAsync(email.Trim(), cancellationToken);
    if (user is null || !user.IsActive)
      return null;

    return passwordHasher.Verify(user, password) ? user : null;
  }
}
