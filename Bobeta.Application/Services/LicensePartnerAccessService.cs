using Bobeta.Application.Common;
using Bobeta.Application.DTOs.Portal;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Enums;

namespace Bobeta.Application.Services;

public sealed class LicensePartnerAccessService(
    ILicensePartnerRepository partners,
    IPlayerRepository players) : ILicensePartnerAccessService
{
  public async Task<IReadOnlyList<string>> GetLicensedCountryCodesAsync(Guid portalUserId, CancellationToken cancellationToken = default)
  {
    var partner = await partners.GetByPortalUserIdAsync(portalUserId, cancellationToken);
    if (partner is null)
      return [];

    return partner.CountryAssignments
      .Where(a => a.IsActive)
      .Select(a => a.CountryCode)
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .ToList();
  }

  public async Task<bool> CanAccessPlayerAsync(
      Guid portalUserId,
      PortalUserRole role,
      Guid playerId,
      CancellationToken cancellationToken = default)
  {
    if (role is PortalUserRole.PlatformOwner or PortalUserRole.Member)
      return true;

    if (role != PortalUserRole.LicensePartner)
      return false;

    var player = await players.GetByIdAsync(playerId, cancellationToken);
    if (player is null)
      return false;

    var playerCountry = player.CountryCode ?? CountryCatalog.ResolveCountryCodeFromPhone(player.PhoneNumber);
    if (string.IsNullOrWhiteSpace(playerCountry))
      return false;

    var licensed = await GetLicensedCountryCodesAsync(portalUserId, cancellationToken);
    return licensed.Contains(playerCountry, StringComparer.OrdinalIgnoreCase);
  }
}
