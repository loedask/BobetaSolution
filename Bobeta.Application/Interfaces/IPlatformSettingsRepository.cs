using Bobeta.Domain.Entities;

namespace Bobeta.Application.Interfaces;

public interface IPlatformSettingsRepository
{
  Task<PlatformSetting?> GetAsync(string key, CancellationToken cancellationToken = default);
  Task UpsertAsync(string key, string value, Guid? updatedByPortalUserId, CancellationToken cancellationToken = default);
}
