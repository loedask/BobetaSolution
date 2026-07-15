using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Bobeta.Persistence.Repositories;

public class PlatformSettingsRepository(BobetaDbContext db) : IPlatformSettingsRepository
{
  public async Task<PlatformSetting?> GetAsync(string key, CancellationToken cancellationToken = default) =>
    await db.PlatformSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

  public async Task UpsertAsync(string key, string value, Guid? updatedByPortalUserId, CancellationToken cancellationToken = default)
  {
    var existing = await db.PlatformSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
    if (existing is null)
    {
      db.PlatformSettings.Add(new PlatformSetting
      {
        Key = key,
        Value = value,
        UpdatedAt = DateTime.UtcNow,
        UpdatedByPortalUserId = updatedByPortalUserId
      });
    }
    else
    {
      existing.Value = value;
      existing.UpdatedAt = DateTime.UtcNow;
      existing.UpdatedByPortalUserId = updatedByPortalUserId;
    }

    await db.SaveChangesAsync(cancellationToken);
  }
}
