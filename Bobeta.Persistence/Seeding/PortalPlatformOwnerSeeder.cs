using Bobeta.Application.Configuration;
using Bobeta.Application.Services;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Bobeta.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bobeta.Persistence.Seeding;

/// <summary>Provisions portal platform owner accounts from <see cref="PortalSettings.PlatformOwnerEmails"/>.</summary>
public static class PortalPlatformOwnerSeeder
{
  public static async Task SeedAsync(
      BobetaDbContext db,
      IOptions<PortalSettings> settings,
      PortalPasswordHasher passwordHasher,
      ILogger logger,
      CancellationToken cancellationToken = default)
  {
    var portalSettings = settings.Value;
    var bootstrapPassword = portalSettings.BootstrapPassword;
    if (string.IsNullOrWhiteSpace(bootstrapPassword))
    {
      logger.LogWarning("Portal bootstrap skipped: Portal:BootstrapPassword is not configured.");
      return;
    }

    foreach (var rawEmail in portalSettings.PlatformOwnerEmails)
    {
      if (string.IsNullOrWhiteSpace(rawEmail))
        continue;

      var email = rawEmail.Trim().ToLowerInvariant();
      if (await db.PortalUsers.AnyAsync(u => u.Email == email, cancellationToken))
        continue;

      var user = new PortalUser
      {
        Id = Guid.NewGuid(),
        Email = email,
        DisplayName = email.Split('@')[0],
        Role = PortalUserRole.PlatformOwner,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
      };
      user.PasswordHash = passwordHasher.Hash(user, bootstrapPassword);

      db.PortalUsers.Add(user);
      logger.LogInformation("Provisioned portal platform owner account for {Email}", email);
    }

    await db.SaveChangesAsync(cancellationToken);
  }
}
