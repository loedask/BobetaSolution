using Bobeta.Domain.Enums;

namespace Bobeta.Application.Interfaces;

public interface ILicensePartnerAccessService
{
  Task<IReadOnlyList<string>> GetLicensedCountryCodesAsync(Guid portalUserId, CancellationToken cancellationToken = default);
  Task<bool> CanAccessPlayerAsync(Guid portalUserId, PortalUserRole role, Guid playerId, CancellationToken cancellationToken = default);
}
